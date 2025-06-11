using Bb;

namespace AILib
{

    public struct Document
    {

        public Document(FileInfo? sourcFile, FileInfo? targetFile, FolderIndex index)
        {
            this._sourceFile = sourcFile;
            this._targetFile = targetFile;
            this.Index = index;
            var path = sourcFile?.FullName;
        }

        public readonly FileInfo? SourceFile => _sourceFile;

       

        public string SourceName => _sourceFile?.FullName ?? string.Empty;

        public int SourceLength => (int)(_sourceFile?.Length ?? 0);

        public int SourceNameLength => _sourceFile?.FullName.Length ?? 0;

        public readonly FileInfo? TargetFile => _targetFile;


        private readonly FileInfo? _sourceFile;
        private readonly FileInfo? _targetFile;

        public FolderIndex Index { get; }

        public static readonly Document Empty = new Document(null, null, new FolderIndex());


    }

}
