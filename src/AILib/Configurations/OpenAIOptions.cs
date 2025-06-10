


namespace AILib.Configurations
{
    public class OpenAIOptions : List<OpenAIOption>
    {


        public OpenAIOptions(int capacity) : base(capacity) { }

        public OpenAIOptions() : base() { }

        public OpenAIOptions(IEnumerable<OpenAIOption> collection) : base(collection) { }

        public OpenAIOption? this[string key] 
        {

            get => this.FirstOrDefault(c => c.Name == key);
            set
            {

                var o = this.FirstOrDefault(c => c.Name == key);
                if (o == null)
                {
                    if (value == null) return;
                    Add(value);
                }
                else if (value == null)
                {

                    var index = this.IndexOf(o);
                    if (value == null)
                        RemoveAt(index);
                    else
                        this[index] = value;
                }

            }
        }

    }


}