﻿using Fonlow.OpenApiClientGen.ClientTypes;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace SwagTests
{
	public class NG2TestHelper : TsTestHelper
	{
		readonly ITestOutputHelper output;
		readonly bool buildToValidate;

		public NG2TestHelper(Type genType, ITestOutputHelper output, bool buildToValidate) : base(genType)
		{
			this.output = output;
			this.buildToValidate = buildToValidate;
		}

		public void GenerateAndAssertAndBuild(string openApiFile, string expectedFile, Settings settings = null)
		{
			string s = TranslateDefToCode(openApiFile, settings ?? new Settings()
			{
				ClientNamespace = "MyNS",
				ContainerClassName = "MyClient", //the TestBed requires this containerClassName
				ContainerNameStrategy = ContainerNameStrategy.None,
				ActionNameStrategy = ActionNameStrategy.Default,
				DataAnnotationsToComments = true,
			});

			//File.WriteAllText(expectedFile, s); //To update Results after some feature changes. Copy what in the bin folder back to the source content.
			string expected = ReadFromResults(expectedFile);
			Assert.Equal(expected, s);

			if (buildToValidate)
			{
				Assert.Equal(0, CheckNGBuild(s));
			}
		}

		int CheckNGBuild(string codes)
		{
			File.WriteAllText(@"..\..\..\..\NG2TestBed\src\clientapi\ClientApiAuto.ts", codes);
			return Build(@"..\..\..\..\NG2TestBed\");
		}

		int Build(string ng2Dir)
		{
			var currentDir = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(ng2Dir); // setting ProcessStartInfo.WorkingDirectory is not always working. Working in this demo, but not working in other heavier .net core Web app.
			var ngCmd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm\\ng.cmd");
			ProcessStartInfo info = new ProcessStartInfo(ngCmd, "build --source-map=false --build-optimizer=false")
			{
				UseShellExecute = false,
				RedirectStandardError = true,
			};

			try
			{
				var process = Process.Start(info);
				var errorMsg = process.StandardError.ReadToEnd(); //before WaitForExit() https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standarderror?view=netcore-3.1#System_Diagnostics_Process_StandardError
				if (!String.IsNullOrEmpty(errorMsg))
				{
					output.WriteLine("Code generated but with ng build errors:");
					output.WriteLine(errorMsg);
				}

				process.WaitForExit();

				return process.ExitCode;
			}
			finally
			{
				Directory.SetCurrentDirectory(currentDir);
			}
		}
	}
}
