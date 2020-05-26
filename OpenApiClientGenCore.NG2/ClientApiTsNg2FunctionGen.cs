﻿using Fonlow.TypeScriptCodeDom;
using System;
using System.CodeDom;
using System.Diagnostics;
using System.Linq;
using Fonlow.OpenApiClientGen.ClientTypes;

namespace Fonlow.CodeDom.Web.Ts
{
	/// <summary>
	/// Generate a client function upon ApiDescription for Angular
	/// </summary>
	public class ClientApiTsNG2FunctionGen : ClientApiTsFunctionGenBase
	{
		//const string NG2HttpResponse = "Response";
		const string NG2HttpBlobResponse = "HttpResponse<Blob>";
		const string NG2HttpStringResponse = "HttpResponse<string>";
		const string ContentOptionsForResponse = "{ headers: headersHandler ? headersHandler().append('Content-Type', 'application/json;charset=UTF-8') : new HttpHeaders({ 'Content-Type': 'application/json;charset=UTF-8' }), observe: 'response', responseType: 'text' }";
		const string OptionsForResponse = "{ headers: headersHandler ? headersHandler() : undefined, observe: 'response', responseType: 'text' }";
		const string GeneralHeaderHandler = "{ headers: headersHandler ? headersHandler() : undefined }";
		const string GeneralHeaderHandlerWithContent = "{ headers: headersHandler ? headersHandler().append('Content-Type', 'application/json;charset=UTF-8') : new HttpHeaders({ 'Content-Type': 'application/json;charset=UTF-8' }) }";
		string returnTypeText = null;
		string contentType;
		readonly Settings settings;

		public ClientApiTsNG2FunctionGen(Settings settings, JSOutput jsOutput) : base()
		{
			this.contentType = jsOutput.ContentType;
			this.settings = settings;
		}

		protected override CodeMemberMethod CreateMethodName()
		{
			returnTypeText = TypeMapper.MapCodeTypeReferenceToTsText(ReturnTypeReference);
			if (returnTypeText == "any" || returnTypeText == "void")
			{
				returnTypeText = NG2HttpStringResponse;// NG2HttpResponse;
			}
			else if (returnTypeText == "response")
			{
				returnTypeText = NG2HttpStringResponse;
			}
			else if (returnTypeText == "blobresponse")
			{
				returnTypeText = NG2HttpBlobResponse;
			}

			var typeCast = returnTypeText == null ? "<HttpResponse<string>>" : $"<{returnTypeText}>";

			var callbackTypeText = $"Observable{typeCast}";
			Debug.WriteLine("callback: " + callbackTypeText);
			var returnTypeReferenceWithObservable = new CodeSnipetTypeReference(callbackTypeText);

			return new CodeMemberMethod()
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				Name = ActionName,
				ReturnType = returnTypeReferenceWithObservable,
			};
		}


		protected override void RenderImplementation()
		{
			var httpMethodName = HttpMethod.ToString().ToLower(); //Method is always uppercase.
																  //deal with parameters
			var parameters = ParameterDescriptions.Select(d =>
				new CodeParameterDeclarationExpression(TypeMapper.MapCodeTypeReferenceToTsText(d.ParameterTypeReference), d.Name))
				.ToArray();

			Method.Parameters.AddRange(parameters);
			if (RequestBodyCodeTypeReference != null)
			{
				var p = new CodeParameterDeclarationExpression(RequestBodyCodeTypeReference, "requestBody");
				Method.Parameters.Add(p);
			}

			if (settings.HandleHttpRequestHeaders)
			{
				Method.Parameters.Add(new CodeParameterDeclarationExpression(
					"() => HttpHeaders", "headersHandler?"));
			}

			var jsUriQuery = UriQueryHelper.CreateUriQueryForTs(RelativePath, ParameterDescriptions);
			var uriText = jsUriQuery == null ? $"this.baseUri + '{RelativePath}'" :
				RemoveTrialEmptyString($"this.baseUri + '{jsUriQuery}'");

			if (ReturnTypeReference != null && ReturnTypeReference.BaseType == "System.String" && ReturnTypeReference.ArrayElementType == null)//stringAsString is for .NET Core Web API
			{
				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, {{ headers: headersHandler ? headersHandler() : undefined, responseType: 'text' }});"));
					return;
				}

				if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (String.IsNullOrEmpty(contentType))
					{
						contentType = "application/json;charset=UTF-8";
					}

					if (RequestBodyCodeTypeReference == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, null, {ContentOptionsForResponse});"));
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, JSON.stringify(requestBody), {ContentOptionsForResponse});"));
					}

					return;
				}

			}
			else if (returnTypeText == NG2HttpBlobResponse)//translated from blobresponse to this
			{
				const string optionForStream = "{ headers: headersHandler ? headersHandler() : undefined, observe: 'response', responseType: 'blob' }";

				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, {optionForStream});"));
					return;
				}

				if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (RequestBodyCodeTypeReference == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, null, {optionForStream});"));
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, JSON.stringify(requestBody), {optionForStream});"));
					}

					return;
				}

			}
			else if (returnTypeText == NG2HttpStringResponse)//translated from response to this
			{
				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, {OptionsForResponse});"));
					return;
				}

				if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (RequestBodyCodeTypeReference == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, null, {OptionsForResponse});"));
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, JSON.stringify(requestBody), {OptionsForResponse});"));
					}

					return;
				}

			}
			else
			{
				var returnTypeCast = returnTypeText==null? String.Empty : $"<{returnTypeText}>";

				if (httpMethodName == "get" || httpMethodName == "delete")
				{
					if (returnTypeText == null)
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, {OptionsForResponse});")); //only http response needed
					}
					else
					{
						Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}{returnTypeCast}({uriText}, {GeneralHeaderHandler});"));
					}
				}
				else if (httpMethodName == "post" || httpMethodName == "put")
				{
					if (returnTypeText == null)//http response
					{
						if (RequestBodyCodeTypeReference == null)//no content body
						{
							Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, null, {OptionsForResponse});"));
						}
						else
						{
							Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}({uriText}, JSON.stringify(requestBody), {ContentOptionsForResponse});"));
						}
					}
					else // type is returned
					{
						if (RequestBodyCodeTypeReference == null)
						{
							Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}{returnTypeCast}({uriText}, null, {GeneralHeaderHandler});"));
						}
						else
						{
							Method.Statements.Add(new CodeSnippetStatement($"return this.http.{httpMethodName}{returnTypeCast}({uriText}, JSON.stringify(requestBody), {GeneralHeaderHandlerWithContent});"));
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
