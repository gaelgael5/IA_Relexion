namespace AILib
{

    public struct Document
    {


        public static readonly Document Empty = new Document(null, null);

        public Document(FileInfo? sourcFile, FileInfo? targetFile)
        {

            this._sourceFile = sourcFile;
            this._targetFile = targetFile;

            var path = sourcFile?.FullName;
            if (!string.IsNullOrEmpty(path))
                this._hashSource = path.ComputeFileHash();
            else
                this._hashSource = string.Empty;

        }

        public readonly FileInfo? SourceFile => _sourceFile;

        public string HashSource => _hashSource;

        public string SourceName => _sourceFile?.FullName ?? string.Empty;

        public string SourceContent => _sourceFile ==null ? string.Empty : File.ReadAllText(_sourceFile.FullName);

        public int SourceLength => (int)(_sourceFile?.Length ?? 0);

        public int SourceNameLength => _sourceFile?.FullName.Length ?? 0;

        public readonly FileInfo? TargetFile => _targetFile;


        private readonly FileInfo? _sourceFile;
        private readonly FileInfo? _targetFile;
        private readonly string _hashSource;

    }

    public static class DocumentExtensions
    {

        public static bool IsEmpty(this Document document)
        {
            return document.SourceFile == null && document.TargetFile == null;
        }

    }

}
