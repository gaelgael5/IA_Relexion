using System.Security.Cryptography;

namespace AILib
{


    public static class FolderParser
    {


        public static IEnumerable<Document> ParseFolder(string sourceFolderPath, string targetFolderPath, Func<string, string> targetFileGenerator, string pattern)
        {

            if (string.IsNullOrEmpty(targetFolderPath))
                throw new ArgumentNullException(nameof(targetFolderPath), "Target folder path cannot be null or empty.");

            var dirSource = new DirectoryInfo(sourceFolderPath);

            if (targetFileGenerator == null)
                targetFileGenerator = (name) => name + ".txt";

            if (string.IsNullOrEmpty(pattern))
                pattern = "*.*";

            var l = sourceFolderPath.Length;
            


            foreach (FileInfo item in dirSource.EnumerateFiles("*.*", SearchOption.AllDirectories))
            {

                item.Refresh();
                if (item.Exists && !item.Attributes.HasFlag(FileAttributes.Hidden))
                {

                    var targetFolder = item.Directory?.FullName.Substring(l) ?? string.Empty;
                    targetFolder = Path.Combine(targetFolderPath, targetFolder);

                    var targetName = Path.GetFileNameWithoutExtension(item.Name);
                    targetName = targetFileGenerator(targetName);

                    var targetFile = new FileInfo(Path.Combine(targetFolder, targetName));
                    targetFile.Refresh();

                    Document itemS = Document.Empty;

                    try
                    {
                        itemS = new Document(item, targetFile);
                    }
                    catch (System.IO.IOException)
                    {

                    }

                    if (!itemS.IsEmpty())
                        yield return itemS;

                }

            }

        }


        internal static string ComputeFileHash(this string path)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

}
