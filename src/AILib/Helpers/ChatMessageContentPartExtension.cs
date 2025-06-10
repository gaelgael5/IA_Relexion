using Bb;
using OpenAI.Chat;

namespace AILib.Helpers
{


    public static class ChatMessageContentPartExtension
    {

        public static bool SaveContent(this ChatMessageContentPart message, string directoryPath, string name)
        {

            if (message == null)
                throw new ArgumentNullException(nameof(message), "ChatMessageContentPart cannot be null.");

            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentNullException(nameof(directoryPath), "Directory target path cannot be null or empty.");
      
            return SaveContent(message, directoryPath.AsDirectory(), name);

        }

        public static bool SaveContent(this ChatMessageContentPart message, DirectoryInfo directory, string name)
        {

            if (message == null)
                throw new ArgumentNullException(nameof(message), "ChatMessageContentPart cannot be null.");

            if (directory == null)
                throw new ArgumentNullException(nameof(directory), "directory target path cannot be null.");

            if (name == null)
            {
                if (message.ImageUri != null)
                    name = message.ImageUri.LocalPath;
                else
                    throw new ArgumentNullException(nameof(name), "File name cannot be null.");
            }

            var n = Path.GetFileNameWithoutExtension(name);
            bool WithoutExtension = n == name;

            // Ensure the directory exists
            directory.Refresh();
            if (!directory.Exists)
                directory.Create();

            n = directory.Combine(n);

            if (!string.IsNullOrEmpty(message.Text))
            {

                if (WithoutExtension)
                    n += ".txt";

                n.Save(message.Text);

                return true;
            }
            if (message.ImageBytes != null)
            {

                if (WithoutExtension)
                {
                    n += message.ImageBytesMediaType.ResolveExtensionFromMediaType();
                }

                File.WriteAllBytes(n, message.ImageBytes.ToArray());

            }           
            else
            {
                System.Diagnostics.Debugger.Break();
            }

            return false;

        }


    }


}