﻿using Fonlow.TypeScriptCodeDom;
using System;
using System.CodeDom;
using System.Diagnostics;
using System.Linq;
using Fonlow.OpenApiClientGen.ClientTypes;

namespace Fonlow.CodeDom.Web.Ts
{
	/// <summary>
	/// Generate a client function upon ApiDescription for Aurelia
	/// </summary>
	public class ClientApiTsFetchFunctionGen : ClientApiTsFunctionGenBase
	{
		const string AureliaHttpResponse = "Response";
		const string AureliatHttpBlobResponse = "Blob";
		const string AureliaHttpStringResponse = "string";
	
		string returnTypeText = null;
		string typeCast = null;
		string contentType;
		readonly Settings settings;

		public ClientApiTsFetchFunctionGen(Settings settings, JSOutput jsOutput) : base()
		{
			this.settings = settings;

			contentType = jsOutput.ContentType;
			if (String.IsNullOrEmpty(contentType))
			{
				contentType = "application/json;charset=UTF-8";
			}
		}

		protected override CodeMemberMethod CreateMethodName()
		{
			returnTypeText = TypeMapper.MapCodeTypeReferenceToTsText(ReturnTypeReference);
			if (returnTypeText == "any" || returnTypeText == "void")
			{
				returnTypeText = AureliaHttpResponse;
			}
			else if (returnTypeText == "response")
			{
				returnTypeText = AureliaHttpResponse;
			}
			else if (returnTypeText == "blobresponse")
			{
				returnTypeText = AureliatHttpBlobResponse;
			}

			typeCast = returnTypeText == null ? "Response" : $"{returnTypeText}";

			string callbackTypeText = $"Promise<{typeCast}>";
			Debug.WriteLine("callback: " + callbackTypeText);
			CodeSnipetTypeReference returnTypeReferenceWithObservable = new CodeSnipetTypeReference(callbackTypeText);

			return new CodeMemberMethod()
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				Name = ActionName,
				ReturnType = returnTypeReferenceWithObservable,
			};
		}


		protected override void RenderImplementation()
		{
			var httpMethodName = HttpMethod.ToString().ToLower();

			string contentOptionsWithHeadersHandlerForString = $"{{ method: '{httpMethodName}', headers: headersHandler ? Object.assign(headersHandler(), {{ 'Content-Type': '{contentType}' }}): {{ 'Content-Type': '{contentType}' }}, body: JSON.stringify(requestBody) }}";
			var ContentOptionsForString = settings.HandleHttpRequestHeaders ? contentOptionsWithHeadersHandlerForString : $"{{ method: '{httpMethodName}', headers: {{ 'Content-Type': '{contentType}' }}, body: JSON.stringify(requestBody) }}";

			string contentOptionsWithHeadersHandlerForResponse = $"{{ method: '{httpMethodName}', headers: headersHandler ? Object.assign(headersHandler(), {{ 'Content-Type': '{contentType}' }}): {{ 'Content-Type': '{contentType}' }}, body: JSON.stringify(requestBody) }}";
			var ContentOptionsForResponse = settings.HandleHttpRequestHeaders ? contentOptionsWithHeadersHandlerForResponse : $"{{ method: '{httpMethodName}', headers: {{ 'Content-Type': '{contentType}' }}, body: JSON.stringify(requestBody) }}";

			string optionsWithHeadersHandlerAndContent = $"{{ method: '{httpMethodName}', headers: headersHandler ? Object.assign(headersHandler(), {{ 'Content-Type': '{contentType}' }}): {{ 'Content-Type': '{contentType}' }}, body: JSON.stringify(requestBody) }}";
			var OptionsWithContent = settings.HandleHttpRequestHeaders ? optionsWithHeadersHandlerAndContent : $"{{ method: '{httpMethodName}', headers: {{ 'Content-Type': '{contentType}' }}, body: JSON.stringify(requestBody) }}";

			string optionsWithHeadersHandlerForString = $"{{ method: '{httpMethodName}', headers: headersHandler ? headersHandler() : undefined }}";
			var OptionsForString = settings.HandleHttpRequestHeaders ? optionsWithHeadersHandlerForString : $"{{ method: '{httpMethodName}' }}";

			string optionsWithHeadersHandlerForResponse = $"{{ method: '{httpMethodName}', headers: headersHandler ? headersHandler() : undefined }}";
			var OptionsForResponse = settings.HandleHttpRequestHeaders ? optionsWithHeadersHandlerForResponse : $"{{ method: '{httpMethodName}' }}";

			string optionsWithHeadersHandler = $"{{ method: '{httpMethodName}', headers: headersHandler ? headersHandler() : undefined }}";
			var Options = settings.HandleHttpRequestHeaders ? optionsWithHeadersHandler : $"{{ method: '{httpMethodName}' }}";

			CodeParameterDeclarationExpression[] parameters = ParameterDescriptions.Select(d =>
				new CodeParameterDeclarationExpression(TypeMapper.MapCodeTypeReferenceToTsText(d.ParameterTypeReference), d.Name))
				.ToArray();

			Method.Parameters.AddRange(parameters);

			if (RequestBodyCodeTypeReference != null)
			{
				CodeParameterDeclarationExpression p = new CodeParameterDeclarationExpression(RequestBodyCodeTypeReference, "requestBody");
				Method.Parameters.Add(p);
			}

			if (settings.HandleHttpRequestHeaders)
			{
				Method.Parameters.Add(new CodeParameterDeclarationExpression(
					"() => {[header: string]: string}", "headersHandler?"));
			}

			string jsUriQuery = UriQueryHelper.CreateUriQueryForTs(RelativePath, ParameterDescriptions);
			string uriText = jsUriQuery == null ? $"this.baseUri + '{RelativePath}'" :
				RemoveTrialEmptyString($"this.baseUri + '{jsUriQuery}'");

			if (ReturnTypeReference != null && ReturnTypeReference.BaseType == "System.String" && ReturnTypeReference.ArrayElementType == null)//stringAsString is for .NET Core Web API
			{
				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {OptionsForString}).then(d => d.text());")); //todo: type cast is not really needed.
					return;
				}

				if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (RequestBodyCodeTypeReference == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {OptionsForString}).then(d => d.text());"));
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {ContentOptionsForString}).then(d => d.text());"));
					}

					return;
				}
			}
			else if (returnTypeText == AureliaHttpStringResponse)//translated from response to this
			{
				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {OptionsForResponse}).then(d => d.text());"));
					return;
				}

				if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (RequestBodyCodeTypeReference == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, null, {OptionsForResponse}).then(d => d.text());"));
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, JSON.stringify(requestBody), {OptionsForResponse});"));
					}

					return;
				}

			}
			else if (returnTypeText == AureliaHttpResponse) // client should care about only status
			{
				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {Options});"));
					return;
				}

				if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (RequestBodyCodeTypeReference == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {Options});"));
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {OptionsWithContent});"));
					}

					return;
				}

			}
			else
			{
				string returnTypeCast = returnTypeText == null ? String.Empty : $"<{returnTypeText}>";

				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					if (returnTypeText == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {OptionsForResponse});")); //only http response needed
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {Options}).then(d => d.json());"));
					}
				}
				else if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (returnTypeText == null)//http response
					{
						if (RequestBodyCodeTypeReference == null)//no content body
						{
							Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {OptionsForResponse});"));
						}
						else
						{
							Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {ContentOptionsForResponse});"));
						}
					}
					else // type is returned
					{
						if (RequestBodyCodeTypeReference == null) // no body
						{
							Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {Options}).then(d => d.json());"));
						}
						else
						{
							Method.Statements.Add(new CodeSnippetStatement($"return fetch({uriText}, {OptionsWithContent}).then(d => d.json());"));
						}
					}
				}
				else
				{
					Debug.Assert(false, $"How come with {httpMethodName}?");
				}
			}


		}

	}

}
