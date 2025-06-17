using AILib;
using AILib.Configurations;
using AILib.Helpers;
using Bb;
using Bb.Configurations;
using Bb.Gits;
using Bb.Schemas;
using ConsoleAI.Helpers;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;

namespace ConsoleAI
{

    public static partial class Commands
    {

        /// <summary>
        /// Download configuration from git and reload the configuration files.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static Command RegisterChat()
        {

            var cmd = new Command("chat", "call service Azure OpenAI in chat mode");
            Option<string> configOpt, gitOpt, dirTargetOpt, dirPatternSourceOpt, dirPatternTargetOpt, azureServiceOpt, promptOpt;
            Option<List<string>> dirSourceOpt;

            cmd.AddOption(configOpt = new Option<string>("--config", "configuration folder"));
            cmd.AddOption(gitOpt = new Option<string>("--git", "connexion string to git container of the configuration"));
            cmd.AddOption(dirSourceOpt = new Option<List<string>>("--parse", "source folder") { AllowMultipleArgumentsPerToken = true });
            cmd.AddOption(dirTargetOpt = new Option<string>("--output", "target folder"));
            cmd.AddOption(dirPatternSourceOpt = new Option<string>("--pattern", "globbing pattern source file and mode. e.g. '*.cs'. you can specify the mode like -folder (all files are group by parent folder) or -all (all files are sent in one shot)"));
            cmd.AddOption(dirPatternTargetOpt = new Option<string>("--name", "extension for target file"));
            cmd.AddOption(azureServiceOpt = new Option<string>("--service", "azure service name"));
            cmd.AddOption(promptOpt = new Option<string>("--prompt", "promps to apply. if the prompt is a file 'file:{filename}' or set directly the prompt"));

            cmd.SetHandler((string config, string git, List<string> sourceFolder, string targetFolder, string patternSource, string outName, string prompt, string azureService) =>
            {
                
                ExecuteChat(LoadConfiguration(config, git, sourceFolder, targetFolder, patternSource, outName, prompt, azureService));

            }, configOpt, gitOpt, dirSourceOpt, dirTargetOpt, dirPatternSourceOpt, dirPatternTargetOpt, promptOpt, azureServiceOpt);

            return cmd;

        }

        private static void ExecuteChat(Context ctx)
        {

            if (ctx.AzureOptions == null)
                throw new InvalidOperationException("Azure options not found in configuration.");

            var service = ctx.AzureOptions.OpenAIServices[ctx.AzureServiceName] ?? throw new InvalidOperationException("OpenAI service 'DevC#' not found in configuration.");
            if (string.IsNullOrEmpty(service.Endpoint)) throw new InvalidOperationException("OpenAI service endpoint is not set in configuration.");

            var chat = service.CreateChatSession();

            Stopwatch stopwatch = new Stopwatch();

            using (var store = new IndexStore($".{ctx.HashPrompt}.index.json"))
            {

                IEnumerable<Document>? items = null;

                if (ctx.Strategy == ParseStrategy.FileByFile)
                    items = FolderParser.ParseFileByFile(store, ctx.DirectorySources, ctx.DirectoryTarget, (name) => name + ctx.OutName, ctx.PatternSource);

                else if (ctx.Strategy == ParseStrategy.ByFolder)
                    items = FolderParser.ParseByFolder(store, ctx.DirectorySources, ctx.DirectoryTarget, (name) => name + ctx.OutName, ctx.PatternSource);

                else if (ctx.Strategy == ParseStrategy.All)
                    items = FolderParser.ParseOneShot(store, ctx.DirectorySources, ctx.DirectoryTarget, (name) => name + ctx.OutName, ctx.PatternSource);

                else
                {
                    System.Diagnostics.Debugger.Break();
                }

                if (items == null)
                    return;

                uint hashCode = 0;
                chat.MustExecute = c =>
                {

                    var o = c.Hash != hashCode;
                    if (!o)
                        $"Execution canceled: files haven't change since last run".WriteWhite();

                    return o;
                };

                foreach (var item in items)
                    if (item.TargetFile != null)
                    {

                        var indexFolder = item.Index.Get(item);
                        hashCode = indexFolder.Hash;

                        stopwatch.Reset();
                        stopwatch.Start();

                        bool s = WorkWithChat(ctx, item, indexFolder, chat);
                        item.Index.SetChanged(s);

                        stopwatch.Stop();
                        $"Executed in : {stopwatch.Elapsed.ToString("c")}".WriteWhite();

                    }

            }

        }

        private static bool WorkWithChat(Context ctx, Document document, FolderIndexDocument indexFolder, ChatSession chat)
        {

            if (document.TargetFile == null)
                return false;


            $"Run on".WriteWhite();
            foreach (var item in document.SourceFiles)
                if (item.Exists)
                    $"  file : '{item.FullName}'... ".WriteWhite();


            var taskMessage = chat.AskOnChat(c =>
            {
                c.Add(Message.CreateUserMessage(ctx.Prompt));

                foreach (var item in document.SourceFiles)
                    c.Add(Message.CreateUserTextAttachedDocument(item.FullName));

            });

            taskMessage.Wait();                 // Wait for the task to complete
            var messages = taskMessage.Result;  // await TaskMessage; // Wait for the task to complete

            if (messages != null)
                return AnalyzeResponse(messages, document, indexFolder, chat);

            return false;

        }


    }

}
