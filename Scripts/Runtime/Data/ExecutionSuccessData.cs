using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{

    /// <summary>
    /// Class representing execution success data.
    /// </summary>
    public class ExecutionSuccessData
    {
        [JsonProperty("prompt_id")]
        public string PromptId { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}