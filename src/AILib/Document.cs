using Bb;

namespace AILib
{

    public struct Document
    {


        public Document(FileInfo? targetFile, FolderIndex index, params FileInfo[] sourceFiles)
        {
            this._sourceFiles = new List<FileInfo>(sourceFiles);
            this._targetFile = targetFile;
            this.Index = index;
        }


        public string GetName()
        {
            if (_hash == null)
            {
                uint hash = 0;
                var sortedFileList = SourceFiles.OrderBy(c => c.Name);
                foreach (var item in sortedFileList)
                    hash ^= Crc32.CalculateCrc32(item?.Name ?? string.Empty);
                _hash = hash.ToString();
            }
            return _hash;
        }

        public long GetLength()
        {
            
            if (_length == null)
            {
                long length = 0;
                foreach (var item in SourceFiles)
                    length += item.Length;
                _length = length;
            }

            return _length.Value;

        }


        public readonly FileInfo? TargetFile => _targetFile;
        
        public FolderIndex Index { get; }

        public static readonly Document Empty = new Document(null, new FolderIndex());

        public readonly List<FileInfo> SourceFiles => _sourceFiles;



        private readonly List<FileInfo> _sourceFiles;
        private readonly FileInfo? _targetFile;
        private string? _hash = null;
        private long? _length = null;


    }

}
