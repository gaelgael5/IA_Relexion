
using Bb;

namespace AILib
{


    public static class FolderParser
    {

        public static IEnumerable<Document> Parse(IndexStore store, DirectoryInfo sourceFolderPath, DirectoryInfo targetFolderPath, Func<string, string> targetFileGenerator, string pattern)
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
                        itemS = new Document(item, targetFile, index);
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


    }

}
