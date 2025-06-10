using OpenAI.Chat;

namespace AILib.Configurations
{
    public class ChatMessages : List<ChatMessage>
    {

        public ChatMessages() : base()
        {

        }

        public ChatMessages(int capacity) : base(capacity)
        {

        }

        public ChatMessages(IEnumerable<ChatMessage> collection) : base(collection)
        {

        }

    }


}