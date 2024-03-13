using wan24.CLI;
using wan24.Core;
using wan24.I8NTool;

await Bootstrap.Async().DynamicContext();
CliConfig.Apply(new(args));
Translation.Current ??= Translation.Dummy;
#if DEBUG
CliApi.DisplayFullExceptions = true;
#endif
CliApi.UseInvokeAuto = false;
CliApi.CommandLine = "wan24I8NTool";
CliApi.HelpHeader = "wan24-I8NTool help\n(c) 2024 Andreas Zimmermann, wan24.de";
return await CliApi.RunAsync(args, exportedApis: [typeof(CliHelpApi), typeof(KeywordExtractorApi), typeof(I8NApi)]).DynamicContext();
