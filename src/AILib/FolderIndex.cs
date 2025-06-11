


using Bb;

namespace AILib
{


    public class FolderIndex : List<FolderIndexDocument>
    {

        public FolderIndex()
        {

        }

        public FolderIndex(IEnumerable<FolderIndexDocument> documents) : base(documents)
        {

        }

        public FolderIndexDocument Get(Document document)
        {

            var name = document.SourceFile?.Name;
            if (!string.IsNullOrEmpty(name))
            {
                var item = this.FirstOrDefault(d => d.Name == name);
                if (item != null)
                    return item;

                item = new FolderIndexDocument();
                this.Add(item);

                return item;
            }

            throw new ArgumentException("Document does not have a valid source file name.", nameof(document));

        }


        public bool Changed { get; private set; }


        public void SetChanged(bool changed)
        {
            if (changed)
                Changed = true;
        }

        public void Save()
        {
            File.FullName.SerializesAndSave(this);
            Changed = false;
        }

        public static FolderIndex Empty => new FolderIndex();

        public FileInfo File { get; internal set; }
    }

}
