using AILib.Configurations;
using Bb;
using Bb.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace ConsoleAI
{


    public class Context
    {

        public Context(string configFolder, DirectoryInfo directorySource, DirectoryInfo directoryTarget, string prompt, string azureService)
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

            DirectorySource = directorySource;
            DirectoryTarget = directoryTarget;
            PatternSource = "*.*";
            OutName = ".txt";
            AzureServiceName = azureService;

            if (prompt.ToLowerInvariant().StartsWith("file:"))
            {
                var filePath = DirectoryPrompts.Combine(prompt.Substring(5).Trim());
                if (File.Exists(filePath))
                    prompt = filePath.LoadFromFile();
            }

            this.Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt), "Prompt cannot be null or empty.");


        }

        public DirectoryInfo DirectoryConfig { get; }
        public string DirectoryPrompts { get; }

        public IConfigurationBuilder Configuration { get; }

        public AzureOptions? AzureOptions { get; internal set; }

        public DirectoryInfo DirectorySource { get; }

        public DirectoryInfo DirectoryTarget { get; }

        public string PatternSource { get; set; }

        public string OutName { get; set; }

        public string AzureServiceName { get; }
        
        public string Prompt { get; }

    }


}
