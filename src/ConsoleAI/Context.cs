using AILib.Configurations;
using Bb;
using Bb.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace ConsoleAI
{


    public class Context
    {

        public Context(string configFolder, DirectoryInfo[] directorySources, DirectoryInfo directoryTarget, string prompt, string azureService, string patternSource)
        {

            DirectoryConfig = (configFolder ?? Directory.GetCurrentDirectory()).AsDirectory();
            if (!DirectoryConfig.Exists)
                DirectoryConfig.Create();

            DirectoryPrompts = DirectoryConfig.Combine("Prompts") ?? throw new InvalidOperationException("Configurations directory not found.");

            // Charge la configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(DirectoryConfig.FullName)
                ;

            //// Init le générateur de schéma
            var _directorySchemas = DirectoryConfig.Combine("Schemas");
            var idTemplate = "http://Black.Beard.com/schema/{0}";
            SchemaGenerator.Initialize(_directorySchemas, idTemplate);

            DirectorySources = directorySources;
            DirectoryTarget = directoryTarget;
            OutName = ".txt";
            AzureServiceName = azureService;

            if (prompt.ToLowerInvariant().StartsWith("file:"))
            {
                var filePath = DirectoryPrompts.Combine(prompt.Substring(5).Trim());
                if (File.Exists(filePath))
                    prompt = filePath.LoadFromFile();
            }

            HashPrompt = prompt.CalculateCrc32(); // Ensure the prompt is processed for CRC32

            PatternSource = patternSource;
            if (patternSource == null || patternSource.Trim().Length == 0)
                PatternSource = "*.*"; // Default pattern
            else
            {
                patternSource = patternSource.ToLowerInvariant().Trim();
                if (patternSource.EndsWith("-folder"))
                {
                    Strategy = ParseStrategy.ByFolder;
                    PatternSource = patternSource.Split(' ')[0];
                }
                else if (patternSource.EndsWith("-all"))
                {
                    Strategy = ParseStrategy.All;
                    PatternSource = patternSource.Split(' ')[0];
                }
            }

            this.Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt), "Prompt cannot be null or empty.");


        }

        public DirectoryInfo DirectoryConfig { get; }
        public string DirectoryPrompts { get; }

        public IConfigurationBuilder Configuration { get; }

        public AzureOptions? AzureOptions { get; internal set; }

        public DirectoryInfo[] DirectorySources { get; }

        public DirectoryInfo DirectoryTarget { get; }
        public uint HashPrompt { get; }
        public string PatternSource { get; set; }

        public string OutName { get; set; }

        public string AzureServiceName { get; }

        public string Prompt { get; }

        public ParseStrategy Strategy { get; }

    }

    public enum ParseStrategy
    {
        FileByFile,
        ByFolder,
        All,
    }

}