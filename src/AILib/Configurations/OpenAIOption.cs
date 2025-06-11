using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.Diagnostics;

namespace AILib.Configurations
{


    [DebuggerDisplay("{Name} : {Type}")]
    public class OpenAIOption
    {

        public OpenAIOption()
        {
            this.Tunes = new OpenAITuneOption();
            Messages = new List<Message>();
            Name = string.Empty;
            Endpoint = string.Empty;
        }

        public string Name { get; set; }

        public string Endpoint { get; set; }

        public string? Region { get; set; }

        public string? ApiKey { get; set; }

        public string Model { get; set; } = "gpt-4o";

        public string? AzureId { get; set; }

        public OpenAITuneOption Tunes { get; set; }

        public List<Message> Messages { get; set; }


        public ChatSession CreateChatSession()
        {

            return new ChatSession(this);

        }


        public AzureOpenAIClient GetClient()
        {

            if (string.IsNullOrWhiteSpace(this.ApiKey))
                return new AzureOpenAIClient(new Uri(Endpoint), new DefaultAzureCredential());


            return new AzureOpenAIClient(new Uri(Endpoint), new Azure.AzureKeyCredential(this.ApiKey));

        }

        public ChatCompletionOptions GetChatCompletionOptions()
        {
            var options = new ChatCompletionOptions
            {
                Temperature = Tunes.Temperature,
                MaxOutputTokenCount = Tunes.MaxOutputTokenCount,

                TopP = Tunes.TopP,
                FrequencyPenalty = Tunes.FrequencyPenalty,
                PresencePenalty = Tunes.PresencePenalty,

                AllowParallelToolCalls = Tunes.AllowParallelToolCalls,
                EndUserId = Tunes.EndUserId,
                IncludeLogProbabilities = Tunes.IncludeLogProbabilities,
                StoredOutputEnabled = Tunes.StoredOutputEnabled,
                TopLogProbabilityCount = Tunes.TopLogProbabilityCount,
            };

            return options;
        }


        public ChatMessages GetChatMessages(Action<List<ChatMessage>>? action = null)
        {

            var messages = new ChatMessages(Messages.Count);

            foreach (var message in Messages)
            {
                if (message.Position == Position.Pre && !string.IsNullOrWhiteSpace(message.Text))
                    messages.Add(message.GetMessage());
            }

            if (action != null)
                action(messages);

            foreach (var message in Messages)
                if (message.Position == Position.Post && !string.IsNullOrWhiteSpace(message.Text))
                    messages.Add(message.GetMessage());
         
            return messages;

        }

    }

}