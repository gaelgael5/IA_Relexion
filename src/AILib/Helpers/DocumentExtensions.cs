namespace AILib
{

    public static class DocumentExtensions
    {

        public static bool IsEmpty(this Document document)
        {
            return document.SourceFiles == null || !document.SourceFiles.Any() || document.TargetFile == null;
        }

    }

}
