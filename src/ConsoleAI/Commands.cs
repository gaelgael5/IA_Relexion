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

    // parameter pattern
    // *.cs
    // *.cs -folder : traite les fichiers par dossier parent
    // *.cs -all    : traite tous les fichiers trouvés dans le dossier et ses sous-dossiers


    // --config "d:\test\ai"
    // --parse "D:\src_pickup\Colis21\src"
    // --output "D:\test\outputc21"
    // --pattern "*.cs"
    // --name ".md"
    // --service "Dev"
    // --prompt "file:Documentation.txt"


    // --config "d:\test\ai" 
    // --parse "D:\src_pickup\Colis21\src" 
    // --output "D:\test\outputc21" 
    // --pattern "*.cs -folder" 
    // --name ".md" --service "Dev" 
    // --prompt "file:DocumentationFonctionnelle.txt"

    // --config "d:\test\ai" 
    // --parse "D:\src_pickup\Colis21\src\Pssa.Colis21.CollectRequestBatch"
    // --parse "D:\src_pickup\Colis21\src\Pssa.Colis21.CollectRequestCore"
    // --parse "D:\src_pickup\Colis21\src\Pssa.Colis21.CollectRequestMsg"
    // --parse "D:\src_pickup\Colis21\src\Pssa.Colis21.CollectRequestWeb"
    // --output "D:\test\outputc21\CollectRequest" 
    // --pattern "*.cs -all" 
    // --name ".md" --service "Dev" 
    // --prompt "file:DocumentationFonctionnelle.txt"


    public static partial class Commands
    {

        static Commands()
        {

            _logger = StaticContainer.Get<ILoggerFactory>()
                .CreateLogger("Commands");

            var assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                var name = assembly.GetName();
                $"Azure AI CLI - {name.Name} v{name.Version}".WriteGreen();
            }

        }

        /// <summary>
        /// Download configuration from git and reload the configuration files.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static RootCommand RegisterOptions(this RootCommand self)
        {

            self.AddCommand(RegisterChat());

            return self;

        }

        private static bool AnalyzeResponse(ChatMessageContent messages, Document document, FolderIndexDocument indexFolder, ChatSession chat)
        {
            bool test = false;
            ChatMessageContentPart? message = messages.FirstOrDefault();

            if (message != null && document.TargetFile != null)
            {

                if (message.SaveContent(document.TargetFile, out string filename))
                {
                    $"result saved : {filename}".WriteWhite();
                    indexFolder.Map(document);
                    indexFolder.Hash = chat.Hash;
                    test = true;
                }
                else
                    $"all results are not saved".WriteRed();

            }

            if (messages.Count > 1)
                System.Diagnostics.Debugger.Break();

            return test;
        }

        private static Context LoadConfiguration(string config, string git, List<string> sourceFolders, string targetFolder, string patternSource, string outName, string prompt, string azureService)
        {

            #region source folders validation
            var _directorySources = new List<DirectoryInfo>(sourceFolders.Count);
            foreach (var sourceFolder in sourceFolders)
            {
                var dir = sourceFolder.AsDirectory() ?? throw new InvalidOperationException($"Source directory '{sourceFolder}' not found.");
                dir.Refresh();
                if (!dir.Exists)
                    throw new InvalidOperationException($"Source directory '{sourceFolder}' does not exist.");
                _directorySources.Add(dir);
            }
            #endregion source folders validation


            #region target folders validation
            var _directoryTarget = targetFolder.AsDirectory() ?? throw new InvalidOperationException("Target directory not found.");
            if (!_directoryTarget.Exists)
                _directoryTarget.Create();

            if (string.IsNullOrEmpty(patternSource))
                patternSource = "*.cs";

            if (string.IsNullOrEmpty(outName))
                outName = ".md";
            #endregion target folders validation


            var ctx = new Context(config, _directorySources.ToArray(), _directoryTarget, prompt, azureService, patternSource)
            {
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


            $"Parse {ctx.PatternSource} from folders :".WriteGreen();
            foreach (var source in ctx.DirectorySources)
                $"  : '{source.FullName}'".WriteGreen();
            $"Anayze with service {ctx.AzureServiceName}".WriteGreen();
            $"using prompt '{ctx.Prompt}'".WriteYellow();
            $"Generate file '{ctx.OutName}' in '{ctx.DirectoryTarget.FullName}'".WriteGreen();


            return ctx;

        }

        private static readonly ILogger _logger;

    }

}
