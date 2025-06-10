using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace AILib.Configurations
{
    public class ChatSession
    {


        public ChatSession(OpenAIOption openAIOption)
        {
            this._openAIOption = openAIOption;
        }



        private ChatClient Initialyze()
        {
            if (_chatClient == null)
            {
                _azureClient = _openAIOption.GetClient();
                _options = _openAIOption.GetChatCompletionOptions();             // Créer des options de complétion de conversation
                _chatClient = _azureClient.GetChatClient(_openAIOption.Model);   // Initialiser le ChatClient avec le nom de déploiement spécifié
            }

            return _chatClient;

        }


        public async Task<ChatMessageContent?> Ask(Action<List<ChatMessage>>? action = null)
        {

            var chat = Initialyze();

            if (chat != null)
            {
                var messages = _openAIOption.GetChatMessages(action);
                ChatCompletion? completion = null;
                try
                {
                    completion = await chat.CompleteChatAsync(messages, _options);
                }
                catch (Exception)
                {
                    throw;
                }

                return completion?.Content;

            }

            throw new InvalidOperationException("ChatClient is not initialized. Please check the OpenAIOption configuration.");

        }


        private OpenAIOption _openAIOption;
        private AzureOpenAIClient? _azureClient;
        private ChatClient? _chatClient;
        private ChatCompletionOptions? _options;
    }
}