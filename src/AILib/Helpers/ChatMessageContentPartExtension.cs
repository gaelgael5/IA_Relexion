using AILib.Configurations;
using Bb;
using OpenAI.Chat;
using System.Diagnostics;

namespace AILib.Helpers
{

    public static class ChatMessageContentPartExtension
    {


        public static uint GetHash(this ChatMessageContent message)
        {

            uint hash = 0;
            foreach (var item in message)
                hash ^= item.GetHash();

            return hash;

        }

        public static uint GetHash(this ChatMessageContentPart messagePart)
        {

            if (!string.IsNullOrEmpty(messagePart.Text))
                return messagePart.Text.CalculateCrc32();

            else if (messagePart.ImageBytes != null)
                return messagePart.ImageBytes.ToArray().CalculateCrc32();

            else
            {
                Debugger.Break();
            }

            return 0;

        }



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

        public static bool SaveContent(this ChatMessageContentPart message, FileInfo file, out string filename)
        {

            if (message == null)
                throw new ArgumentNullException(nameof(message), "ChatMessageContentPart cannot be null.");

            filename = Path.GetFileNameWithoutExtension(file.Name);
            bool WithoutExtension = filename == file.Name;
            string extension = ".txt";
            if (!WithoutExtension)
                extension = file.Extension;

            if (file.Directory == null)
                throw new ArgumentNullException(nameof(file), "File name cannot be null.");

            // Ensure the directory exists
            file.Directory.Refresh();
            if (!file.Directory.Exists)
                file.Directory.Create();

            filename = file.Directory.Combine(filename);

            if (!string.IsNullOrEmpty(message.Text))
            {

                if (WithoutExtension)
                {
                    filename += extension;
                    filename.Save(message.Text);
                }
                else
                {
                    file.Save(message.Text);
                }


                return true;
            }
            if (message.ImageBytes != null)
            {

                var mime = message.ImageBytesMediaType.ResolveExtensionFromMediaType();

                if (WithoutExtension)
                    filename += mime;

                else
                {

                }

                File.WriteAllBytes(filename, message.ImageBytes.ToArray());

            }
            else
            {
                System.Diagnostics.Debugger.Break();
            }

            return false;

        }



    }


}