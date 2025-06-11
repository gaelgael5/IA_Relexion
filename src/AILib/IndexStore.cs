
using Bb;

namespace AILib
{

    public class IndexStore : IDisposable
    {

        public IndexStore()
        {
            this._dic = new Dictionary<string, FolderIndex>(StringComparer.OrdinalIgnoreCase);
        }


        public FolderIndex GetOrCreate(FileInfo file)
        {

            var file1 = file.Directory.Combine(".index.json").AsFile();

            if (!this._dic.TryGetValue(file1.FullName, out var index))
            {
                file1.Refresh();
                if (file1.Exists)
                    index = file1.LoadFromFileAndDeserializes<FolderIndex>();
                else
                    index = new FolderIndex() { File = file1 };

                _dic.Add(file1.FullName, index);

            }

            return index;

        }


        public void Save()
        {

            foreach (var item in _dic)
                if (item.Value.Changed)
                    item.Value.Save();

        }


        private readonly Dictionary<string, FolderIndex> _dic;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Save();
                }

                disposedValue = true;

            }
        }

        public void Dispose()
        {
            // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
