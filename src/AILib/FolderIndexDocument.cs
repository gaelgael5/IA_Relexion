namespace AILib
{
    public class FolderIndexDocument
    {

        public FolderIndexDocument()
        {

        }

        public void Map(Document item)
        {
            this.Name = item.SourceFile?.Name;
            this.Length = item.SourceLength;
        }

        public string? Name { get; set; }

        public uint Hash { get; set; }

        public int? Length { get; set; }

    }
}
