using AILib;
using AILib.Configurations;
using AILib.Helpers;
using Bb;
using Bb.Configurations;
using Bb.Schemas;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace IALibTest
{

    // create a key : // https://platform.openai.com/account/api-keys
    // watch the consommation : https://platform.openai.com/account/usage

    public class UnitTest1
    {

        public UnitTest1()
        {

            // on créé les répertoires de travail
            _directoryBin = new DirectoryInfo(Directory.GetCurrentDirectory());
            _directoryProject = (_directoryBin?.Parent?.Parent?.Parent) ?? throw new InvalidOperationException("Parent directory not found.");
            _directoryGit = (_directoryProject?.Parent?.Parent) ?? throw new InvalidOperationException("Parent directory not found.");
            _directoryConfigurations = _directoryBin.Combine("Configs") ?? throw new InvalidOperationException("Configurations directory not found.");
            _directorySchemas = _directoryConfigurations.Combine("Schemas") ?? throw new InvalidOperationException("Configurations directory not found.");
            _directoryPrompts = _directoryConfigurations.Combine("Prompts") ?? throw new InvalidOperationException("Configurations directory not found.");


            // Init le générateur de schéma
            var idTemplate = "http://Black.Beard.com/schema/{0}";
            SchemaGenerator.Initialize(_directorySchemas, idTemplate);

            // Charge la configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(_directoryConfigurations)
                //.DownloadConfiguration("url=https://github.com/Configurations/Proxmox.git")
                .LoadConfiguration("*.json")
                .Build()
                ;

            config.ResolveConfiguration(out _openAiOptions)
                ;

            if (_openAiOptions == null)
                throw new InvalidOperationException("OpenAI options not found in configuration.");

        }

        [Fact]
        public void ParseTest()
        {

            var pathTarget = Path.Combine(_directoryGit.FullName, "Documentation");

            FolderParser.ParseFileByFile(new IndexStore(), [_directoryProject], [], pathTarget.AsDirectory(), (name) => name + ".md", "*.cs")
                .ToList()
                .ForEach(item =>
                {
                    Assert.NotNull(item.SourceFiles);
                    Assert.NotNull(item.TargetFile);
                });

        }

        [Fact]
        public async Task TestChatGptClient()
        {

            if (_openAiOptions == null)
                throw new InvalidOperationException("OpenAI options not found in configuration.");

            string prompt = @"Voici un fichier C# :
public class Test 
{
    public void DoWork() {
        Console.WriteLine(""Hello"");
    }
}
Peux-tu me proposer une version plus claire ?";


            var service = _openAiOptions.OpenAIServices["Dev"] // Récupérer le point de terminaison OpenAI à partir de variables d’environnement
            ?? throw new InvalidOperationException("OpenAI service 'DevC#' not found in configuration.")
            ;

            if (string.IsNullOrEmpty(service.Endpoint))
                throw new InvalidOperationException("OpenAI service endpoint is not set in configuration.");

            var azureClient = service.GetClient();
            ChatClient chatClient = azureClient.GetChatClient(service.Model);   // Initialiser le ChatClient avec le nom de déploiement spécifié
            var options = service.GetChatCompletionOptions();                   // Créer des options de complétion de conversation
            var messages = service.GetChatMessages();

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);                      // Créer la demande d’achèvement de la conversation
            ChatCompletion completion1 = await chatClient.CompleteChatAsync(Message.CreateUserMessage(prompt));

        }


        [Fact]
        public async Task TestWithFileToDocument()
        {

            if (_openAiOptions == null)
                throw new InvalidOperationException("OpenAI options not found in configuration.");

            var pathToParse = _directoryGit.Combine("src", "AILib", "Helpers", "OpenPdfExtensions.cs").AsFile(); // Chemin vers le fichier à analyser


            var service = _openAiOptions.OpenAIServices["Dev"] // Récupérer le point de terminaison OpenAI Azure
                        ?? throw new InvalidOperationException("OpenAI service 'DevC#' not found in configuration.");
            if (string.IsNullOrEmpty(service.Endpoint))
                throw new InvalidOperationException("OpenAI service endpoint is not set in configuration.");


            var chat = service.CreateChatSession();
            var messages = await chat.AskOnChat(c =>
            {
                c.Add(Message.CreateUserMessageFromFile(_directoryPrompts.Combine("Documentation.txt")));
                c.Add(Message.CreateUserTextDocument(pathToParse));
            });

            if (messages != null)
            {
                foreach (ChatMessageContentPart? message in messages)
                {

                    message.SaveContent(_directoryBin, "OpenPdfExtensions.md");

                    if (!string.IsNullOrEmpty(message.Text))
                    {
                        // _directoryBin.Combine("OpenPdfExtensions.cs").Save(message.Text);
                    }
                    else
                    {

                    }

                    Console.WriteLine(message.Text);
                }
            }
        }


        private readonly DirectoryInfo _directoryBin;
        private readonly DirectoryInfo _directoryProject;
        private readonly DirectoryInfo _directoryGit;
        private readonly string _directoryConfigurations;
        private readonly string _directorySchemas;
        private readonly string _directoryPrompts;
        private readonly AzureOptions? _openAiOptions;

    }


}

