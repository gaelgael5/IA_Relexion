using AILib.Helpers;
using Bb;
using OpenAI.Chat;
using System.Text;

namespace AILib.Configurations
{  


    public class Message
    {

        public Message()
        {
            Texts = new List<string>();
        }

        public Position Position { get; set; } = Position.Pre;

        public ChatMessageType Type { get; set; } = ChatMessageType.SystemChatMessage;

        public List<string> Texts { get; set; }

        public ChatMessage GetMessage()
        {

            return Type switch
            {
                ChatMessageType.UserChatMessage => CreateUserMessage(Text),
                ChatMessageType.AssistantChatMessage => new AssistantChatMessage(ChatMessageContentPart.CreateTextPart(Text)),
                //ChatMessageType.ToolChatMessage => new ToolChatMessage(ChatMessageContentPart.CreateTextPart(Text)),
                //ChatMessageType.FunctionChatMessage => new FunctionChatMessage(ChatMessageContentPart.CreateTextPart(Text)),
                _ => CreateSystemMessage(Text)
            };

        }

        public string Text
        {
            get
            {
                if (sb == null)
                    sb = new StringBuilder(string.Join(Environment.NewLine, Texts));
                return sb.ToString();
            }
        }

        public static ChatMessage CreateUserMessage(string text)
        {
            return new UserChatMessage(ChatMessageContentPart.CreateTextPart(text));
        }

        public static ChatMessage CreateUserMessageFromFile(string path)
        {

            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException($"argument {nameof(path)} is null");

            return CreateUserMessageFromFile(path.AsFile());
        }

        public static ChatMessage CreateUserMessageFromFile(FileInfo path)
        {

            if (path == null)
                throw new ArgumentNullException($"argument {nameof(path)} is null");

            if (!path.Exists)
                throw new FileNotFoundException($"The file {path.FullName} does not exist.");

            return new UserChatMessage(ChatMessageContentPart.CreateTextPart(path.LoadFromFile()));

        }

        public static ChatMessage CreateUserTextAttachedDocument(string path)
        {

            var file = path.AsFile();

            if (!file.Exists)
                throw new FileNotFoundException($"The file {file.FullName} does not exist.");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("```  -- file : " + file.FullName);
            sb.AppendLine(file.LoadFromFile());
            sb.AppendLine("```");
            sb.AppendLine();

            return new UserChatMessage(ChatMessageContentPart.CreateTextPart(sb.ToString()));

        }

        public static ChatMessage CreateUserTextDocument(FileInfo file)
        {
            if (!file.Exists)
                throw new FileNotFoundException($"The file {file.FullName} does not exist.");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- file : " + file.FullName);
            sb.AppendLine(file.LoadFromFile());
            sb.AppendLine("-- end file : " + file.FullName);
            sb.AppendLine();

            return new UserChatMessage(ChatMessageContentPart.CreateTextPart(sb.ToString()));

        }

        public static ChatMessage CreateUserImage(Uri imageUri)
        {
            return new UserChatMessage(ChatMessageContentPart.CreateImagePart(imageUri));
        }

        public static ChatMessage CreateUserImage(Uri imageUri, ChatImageDetailLevel? detailLevel = null)
        {
            return new UserChatMessage(ChatMessageContentPart.CreateImagePart(imageUri, detailLevel));
        }

        public static ChatMessage CreateUserImage(string path, ChatImageDetailLevel? detailLevel = null)
        {
            var (image, mediaType) = ImageExtensions.LoadImageAndDetectMediaType("images/diagramme.png");
            return new UserChatMessage(ChatMessageContentPart.CreateImagePart(new BinaryData(image), mediaType, detailLevel));
        }

        public static List<ChatMessage> CreatePptx(string filePath)
        {

            var slidesText = OpenXmlExtensions.ExtractTextFromSlides(filePath);

            var messages = new List<ChatMessage>();

            for (int i = 0; i < slidesText.Count; i++)
                messages.Add(CreateUserMessage($"Slide {i + 1}:\n{slidesText[i]}"));

            return messages;

        }

        public static List<ChatMessage> CreateWord(string filePath)
        {

            var slidesText = OpenXmlExtensions.ExtractTextFromWordDocument(filePath);

            var messages = new List<ChatMessage>();

            for (int i = 0; i < slidesText.Count; i++)
                messages.Add(CreateUserMessage($"Slide {i + 1}:\n{slidesText[i]}"));

            return messages;

        }

        public static ChatMessage CreateSystemMessage(string text)
        {
            return new SystemChatMessage(ChatMessageContentPart.CreateTextPart(text));
        }

        private StringBuilder? sb;

    }


    public enum Position
    {

        Pre,
        Post

    }

    public enum ChatMessageType
    {
        SystemChatMessage,
        UserChatMessage,
        AssistantChatMessage,
        ToolChatMessage,
        FunctionChatMessage,
    }


}