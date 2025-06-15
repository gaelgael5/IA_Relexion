
using Bb;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AILib
{


    public static class FolderParser
    {

        public static IEnumerable<Document> ParseFileByFile(IndexStore store, DirectoryInfo sourceFolderPath, DirectoryInfo targetFolderPath, Func<string, string> targetFileGenerator, string pattern)
        {


            if (targetFolderPath == null)
                throw new ArgumentNullException(nameof(targetFolderPath), "Target folder path cannot be null or empty.");

            if (targetFileGenerator == null)
                targetFileGenerator = (name) => name + ".txt";

            if (string.IsNullOrEmpty(pattern))
                pattern = "*.*";

            var l = sourceFolderPath.FullName.Length;

            targetFolderPath.Refresh();
            if (!targetFolderPath.Exists)
                targetFolderPath.Create();
            targetFolderPath.Refresh();


            FolderIndex? lastIndex = null;
            foreach (FileInfo item in sourceFolderPath.EnumerateFiles(pattern, SearchOption.AllDirectories))
            {

                item.Refresh();
                if (item.Exists && !item.Attributes.HasFlag(FileAttributes.Hidden))
                {

                    var targetFolder = item.Directory?.FullName.Substring(l).Trim('\\') ?? string.Empty;
                    targetFolder = targetFolderPath.Combine(targetFolder);

                    var targetName = Path.GetFileNameWithoutExtension(item.Name);
                    targetName = targetFileGenerator(targetName);

                    var targetFile = new FileInfo(Path.Combine(targetFolder, targetName));
                    targetFile.Refresh();


                    Document itemS = Document.Empty;
                    FolderIndex index = store.GetOrCreate(targetFile);

                    if (lastIndex != null && lastIndex != index && lastIndex.Changed)
                        lastIndex.Save();
                    lastIndex = index;

                    try
                    {
                        itemS = new Document(targetFile, index, item);
                    }
                    catch (System.IO.IOException)
                    {

                    }

                    if (!itemS.IsEmpty())
                        yield return itemS;

                }

            }

            store.Save();

        }


        public static IEnumerable<Document> ParseByFolder(IndexStore store, DirectoryInfo sourceFolderPath, DirectoryInfo targetFolderPath, Func<string, string> targetFileGenerator, string pattern)
        {


            if (targetFolderPath == null)
                throw new ArgumentNullException(nameof(targetFolderPath), "Target folder path cannot be null or empty.");

            if (targetFileGenerator == null)
                targetFileGenerator = (name) => name + ".txt";

            if (string.IsNullOrEmpty(pattern))
                pattern = "*.*";

            var l = sourceFolderPath.FullName.Length;

            targetFolderPath.Refresh();
            if (!targetFolderPath.Exists)
                targetFolderPath.Create();
            targetFolderPath.Refresh();


            FolderIndex? lastIndex = null;

            foreach (DirectoryInfo dir in sourceFolderPath.EnumerateDirectories("*.", SearchOption.AllDirectories))
            {

                var targetFolder = dir.FullName.Substring(l).Trim('\\') ?? string.Empty;
                targetFolder = targetFolderPath.Combine(targetFolder);

                var targetName = targetFileGenerator(dir.Name);

                var targetFile = new FileInfo(Path.Combine(targetFolder, targetName));
                targetFile.Refresh();

                Document itemS = Document.Empty;
                FolderIndex index = store.GetOrCreate(targetFile);

                if (lastIndex != null && lastIndex != index && lastIndex.Changed)
                    lastIndex.Save();
                lastIndex = index;

                List<FileInfo> files = new List<FileInfo>();


                foreach (FileInfo file in dir.EnumerateFiles(pattern, SearchOption.AllDirectories))
                {

                    file.Refresh();
                    if (file.Exists && !file.Attributes.HasFlag(FileAttributes.Hidden))
                        files.Add(file);

                }

                itemS = new Document(targetFile, index, files.ToArray());

                if (!itemS.IsEmpty())
                    yield return itemS;


            }
            store.Save();

        }


    }

}
