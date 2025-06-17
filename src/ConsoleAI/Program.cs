// dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
// See https://aka.ms/new-console-template for more information



using Bb;
using Bb.Logging.NLog;
using ConsoleAI;
using System.CommandLine;


NLogInitializer.Execute();

var rootCommand = new RootCommand("Azure AI cli")
    .RegisterOptions()
    ;

var result = await rootCommand.InvokeAsync(args);

if (result != 0)
{
    Console.WriteLine($"Command failed with exit code {result}");
    return;
}