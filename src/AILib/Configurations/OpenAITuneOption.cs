namespace AILib.Configurations
{
    public class OpenAITuneOption
    {

        public float Temperature { get; set; } = 0.7f;

        public int MaxOutputTokenCount { get; set; } = 128000;
        
        public float TopP { get; set; } = 0.95f;
        
        public float FrequencyPenalty { get; set; } = 0f;
        
        public float PresencePenalty { get; set; } = 0f;



        public bool? AllowParallelToolCalls { get; set; } = null;
        
        public string? EndUserId = null;
        
        public bool? IncludeLogProbabilities = null;
        
        public bool? StoredOutputEnabled = null;
        
        public int? TopLogProbabilityCount = null;

    }


}