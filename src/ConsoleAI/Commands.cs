using AILib;
using AILib.Configurations;
using AILib.Helpers;
using Bb;
using Bb.Configurations;
using Bb.Gits;
using Bb.Schemas;
using Microsoft.Identity.Client;
using OpenAI.Chat;
using System.CommandLine;

namespace ConsoleAI
{


    // --config "d:\test\ai" --parse "D:\src_pickup\Colis21\src" --output "D:\test\outputc21" --pattern "*.cs" --name ".md" --service "Dev" --prompt "file:Documentation.txt"

    public static class Commands
    {

        /// <summary>
        /// Download configuration from git and reload the configuration files.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static RootCommand RegisterOptions(this RootCommand self)
        {

            var configOption = new Option<string>("--config", "configuration folder");
            self.AddOption(configOption);

            var gitOption = new Option<string>("--git", "connexion string to git container of the configuration");
            self.AddOption(gitOption);

            var dirSourceOption = new Option<string>("--parse", "source folder");
            self.AddOption(dirSourceOption);

            var dirTargetOption = new Option<string>("--output", "target folder");
            self.AddOption(dirTargetOption);

            var dirPatternSourceOption = new Option<string>("--pattern", "globbing pattern source file");
            self.AddOption(dirPatternSourceOption);

            var dirPatternTargetOption = new Option<string>("--name", "extension for target file");
            self.AddOption(dirPatternTargetOption);

            var azureServiceOption = new Option<string>("--service", "azure service name");
            self.AddOption(azureServiceOption);

            var promptOption = new Option<string>("--prompt", "promps to apply. if the prompt is a file 'file:{filename}' or set directly the prompt");
            self.AddOption(promptOption);


            self.SetHandler((string config, string git, string sourceFolder, string targetFolder, string patternSource, string outName, string prompt, string azureService) =>
            {

                var ctx = LoadConfiguration(config, git, sourceFolder, targetFolder, patternSource, outName, prompt, azureService);
                Execute(ctx);

            }, configOption, gitOption, dirSourceOption, dirTargetOption, dirPatternSourceOption, dirPatternTargetOption, promptOption, azureServiceOption);

            return self;

        }

        private static void Execute(Context ctx)
        {

            if (ctx.AzureOptions == null)
                throw new InvalidOperationException("Azure options not found in configuration.");

            var service = ctx.AzureOptions.OpenAIServices[ctx.AzureServiceName] ?? throw new InvalidOperationException("OpenAI service 'DevC#' not found in configuration.");
            if (string.IsNullOrEmpty(service.Endpoint)) throw new InvalidOperationException("OpenAI service endpoint is not set in configuration.");

            var chat = service.CreateChatSession();

            using (var store = new IndexStore())
            {

                var items = FolderParser.Parse(store, ctx.DirectorySource, ctx.DirectoryTarget, (name) => name + ctx.OutName, ctx.PatternSource);

                uint hashCode = 0;
                chat.MustExecute = c => c.Hash != hashCode;

                foreach (var item in items)
                    if (item.TargetFile != null)
                    {

                        var indexFolder = item.Index.Get(item);
                        hashCode = indexFolder.Hash;

                        bool s = Work(ctx, item, indexFolder, chat);
                        item.Index.SetChanged(s);

                    }

            }

        }

        private static bool Work(Context ctx, Document document, FolderIndexDocument indexFolder, ChatSession chat)
        {

            if (document.TargetFile == null)
                return false;

            var taskMessage = chat.Ask(c =>
            {
                c.Add(Message.CreateUserMessage(ctx.Prompt));
                c.Add(Message.CreateUserTextAttachedDocument(document.SourceName));
            });
            taskMessage.Wait(); // Wait for the task to complete
            var messages = taskMessage.Result; //await TaskMessage; // Wait for the task to complete

            if (messages != null)
            {

                bool test = false;

                foreach (ChatMessageContentPart? message in messages)
                {

                    message.SaveContent(document.TargetFile);

                    indexFolder.Map(document);
                    indexFolder.Hash = chat.Hash;

                    test = true;

                }

                return test;

            }


            return false;

        }

        private static Context LoadConfiguration(string config, string git, string sourceFolder, string targetFolder, string patternSource, string outName, string prompt, string azureService)
        {

            var _directorySource = sourceFolder.AsDirectory() ?? throw new InvalidOperationException("Source directory not found.");
            if (!_directorySource.Exists)
                throw new InvalidOperationException("Source directory does not exist.");

            var _directoryTarget = targetFolder.AsDirectory() ?? throw new InvalidOperationException("Target directory not found.");
            if (!_directoryTarget.Exists)
                _directoryTarget.Create();

            if (string.IsNullOrEmpty(patternSource))
                patternSource = "*.cs";

            if (string.IsNullOrEmpty(outName))
                outName = ".md";

            var ctx = new Context(config, _directorySource, _directoryTarget, prompt, azureService)
            {
                PatternSource = patternSource,
                OutName = outName,
            };

            var configBuilder = ctx.Configuration;

            if (!string.IsNullOrEmpty(git))
                configBuilder.DownloadConfiguration(git, ctx.DirectoryConfig);

            configBuilder.LoadConfiguration("*.json");

            var datas = configBuilder.Build();

            if (datas.TryResolveConfiguration(out AzureOptions? openAiOptions) && openAiOptions != null)
                ctx.AzureOptions = openAiOptions;

            else
                throw new InvalidOperationException("configuration not found");

            return ctx;

        }


    }

}
