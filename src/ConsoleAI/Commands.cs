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

    /// chat
    // --config "d:\test\ai" 
    // --parse "D:\src_pickup\Colis21\src\Pssa.Colis21.DatawarehouseBatch"
    // --parse "D:\src_pickup\Colis21\src\Pssa.Colis21.DatawarehouseCore"
    // --parse "D:\src_pickup\Colis21\src\Pssa.Colis21.DatawarehouseMsg"
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

        private static Context LoadConfiguration(string config, string git, List<string> sources, string targetPath, string patternSource, string outName, string prompt, string azureService)
        {

            #region source validation
            var _directorySources = new List<DirectoryInfo>(sources.Count);
            var _fileSources = new List<FileInfo>(sources.Count);
            foreach (var sourceFolder in sources)
            {

                switch (sourceFolder.EvaluateDocument())
                {

                    case IOType.File:
                        var file = sourceFolder.AsFile()
                            ?? throw new InvalidOperationException($"Source file '{sourceFolder}' not found.");
                        file.Refresh();
                        if (!file.Exists)
                            throw new InvalidOperationException($"Source file '{sourceFolder}' does not exist.");
                        _fileSources.Add(file);
                        break;

                    case IOType.Folder:
                        var dir = sourceFolder.AsDirectory()
                            ?? throw new InvalidOperationException($"Source directory '{sourceFolder}' not found.");
                        dir.Refresh();
                        if (!dir.Exists)
                            throw new InvalidOperationException($"Source directory '{sourceFolder}' does not exist.");
                        _directorySources.Add(dir);
                        break;

                    case IOType.None:
                    default:
                        break;
                }

          
            }
            #endregion source validation


            #region target folders validation


            


            if (string.IsNullOrEmpty(patternSource))
                patternSource = "*.cs";

            if (string.IsNullOrEmpty(outName))
                outName = ".md";
            #endregion target folders validation


            var ctx = new Context(config, _directorySources.ToArray(), _fileSources.ToArray(), targetPath, prompt, azureService, patternSource)
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
            $"Generate file '{ctx.OutName}' in '{ctx.TargetDirectory.FullName}'".WriteGreen();


            return ctx;

        }

        private static readonly ILogger _logger;

    }

}
