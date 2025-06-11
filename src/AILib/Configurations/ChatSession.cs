using Azure.AI.OpenAI;
using OpenAI.Chat;
using System;

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


        public Func<ChatSession, bool>? MustExecute { get; set; }

        public ChatMessages Messages { get; private set; }

        public uint Hash { get; private set; }

        public async Task<ChatMessageContent?> Ask(Action<List<ChatMessage>>? action = null)
        {

            var chat = Initialyze();

            if (chat != null)
            {

                Messages = _openAIOption.GetChatMessages(action);
                this.Hash = Messages.GetHash();

                if (MustExecute != null && !MustExecute(this))
                    return null; // Skip execution if MustExecute condition is not met

                ChatCompletion? completion = null;
                try
                {
                    completion = await chat.CompleteChatAsync(Messages, _options);
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