// dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
// See https://aka.ms/new-console-template for more information


using ConsoleAI;
using System.CommandLine;


var rootCommand = new RootCommand("Azure AI cli")
    .RegisterOptions()
    ;


return await rootCommand.InvokeAsync(args);

