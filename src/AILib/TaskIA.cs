using System.Text;

namespace AILib
{
    public class TaskIA
    {

        static TaskIA()
        {
            _documentLenght = _startDocument.Length
                            + _endDocument.Length
                            + _newLineLenght
                            + _newLineLenght
                            + _newLineLenght;
        }

        public TaskIA()
        {
            this._promptStart = new();
            this._promptEnd = new();
            this._documents = new();
            this._lenght = 0;
        }


        public TaskIA ClearPrePrompt()
        {
            _sbPromptStart = null;
            _promptStart.Clear();
            this._lenght = 0;
            return this;
        }

        public TaskIA WithPrePrompt(params string[] strings)
        {

            if (strings == null || strings.Length == 0)
                throw new ArgumentNullException(nameof(strings), "Pre-prompt cannot be null or empty.");

            _sbPromptStart = null;

            foreach (var str in strings)
                if (!string.IsNullOrWhiteSpace(str))
                    _promptStart.Add(str.Trim());

            return this;
        }

        public StringBuilder GetPrePrompt()
        {

            if (_sbPromptStart == null)
            {
                _sbPromptStart = new StringBuilder(_promptStart.Count * 150); // Assuming average length of 150 characters per prompt
                foreach (var str in _promptStart)
                    _sbPromptStart.AppendLine(str);
            }

            return _sbPromptStart;
        }        



        public TaskIA ClearPostPrompt()
        {
            _sbPromptEnd = null;
            _promptEnd.Clear();
            this._lenght = 0;
            return this;
        }

        public TaskIA WithPostPrompt(params string[] strings)
        {

            if (strings == null || strings.Length == 0)
                throw new ArgumentNullException(nameof(strings), "Post-prompt cannot be null or empty.");

            _sbPromptEnd = null;

            foreach (var str in strings)
                if (!string.IsNullOrWhiteSpace(str))
                    _promptEnd.Add(str.Trim());

            return this;
        }

        public StringBuilder GetPostPrompt()
        {

            if (_sbPromptEnd == null)
            {
                _sbPromptEnd = new StringBuilder(_promptStart.Count * 150); // Assuming average length of 150 characters per prompt
                foreach (var str in _promptEnd)
                    _sbPromptEnd.AppendLine(str);
            }

            return _sbPromptEnd;
        }
        


        public TaskIA ClearDocuments()
        {
            _documents.Clear();
            this._lenght = 0; // Reset length to recompute it later
            return this;
        }

        public TaskIA AddDocument(params Document[] items)
        {
            if (items == null || items.Length == 0)
                throw new ArgumentNullException(nameof(items), "Document cannot be null or empty.");

            _documents.AddRange(items);
            this._lenght = 0; // Reset length to recompute it later
            return this;
        }

        public int ComputeDocumentLenght()
        {

            if (this._lenght == 0)
            {
                foreach (var str in _promptStart)
                    this._lenght += str.Length + _newLineLenght;

                foreach (var str in _promptEnd)
                    this._lenght += str.Length + _newLineLenght;
            }

            return this._lenght;

        }
     


        public StringBuilder CreatePrompt()
        {

            var l = this._lenght + _newLineLenght;
            foreach (var item in _documents)
                l += item.SourceNameLength
                  + (int)item.SourceLength
                  + _documentLenght;

            StringBuilder sb = new StringBuilder(l);

            foreach (var str in _promptStart)
                sb.AppendLine(str);

            foreach (var item in _documents)
            {
                sb.Append(_startDocument);
                sb.AppendLine(item.SourceName);
                sb.AppendLine(item.SourceContent);
                sb.AppendLine(_endDocument);
            }

            sb.AppendLine();
            foreach (var str in _promptEnd)
                sb.AppendLine(str);

            return sb;
        }


        private StringBuilder? _sbPromptStart;
        private StringBuilder? _sbPromptEnd;

        private const string _startDocument = "> item : ";
        private const string _endDocument = "> eof";

        private static readonly int _documentLenght;

        private static readonly int _newLineLenght = Environment.NewLine.Length;
        private readonly List<string> _promptStart;
        private readonly List<string> _promptEnd;
        private readonly List<Document> _documents;
        private int _lenght;

    }

}
