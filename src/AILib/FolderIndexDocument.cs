namespace AILib
{
    public class FolderIndexDocument
    {

        public FolderIndexDocument()
        {

        }

        public void Map(Document item)
        {
            this.Name = item.GetName();
            this.Length = item.GetLength();
        }

        public string? Name { get; set; }

        public uint Hash { get; set; }

        public long? Length { get; set; }

    }
}
