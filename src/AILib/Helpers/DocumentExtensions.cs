namespace AILib
{
    public static class DocumentExtensions
    {

        public static bool IsEmpty(this Document document)
        {
            return document.SourceFile == null && document.TargetFile == null;
        }

    }

}
