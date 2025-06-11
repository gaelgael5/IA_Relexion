using AILib.Helpers;
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

        public uint GetHash()
        {
            uint hash = 0;
            foreach (var message in this)
                hash ^= message.Content.GetHash();
            return hash;

        }


    }


}