using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{
    /// <summary>
    /// Class representing the data for "executed" and similar types.
    /// </summary>
    public class ExecutionData
    {
        [JsonProperty("node")]
        public string Node { get; set; }

        [JsonProperty("display_node")]
        public string DisplayNode { get; set; }

        [JsonProperty("output")]
        public ExecutionOutput Output { get; set; }

        [JsonProperty("prompt_id")]
        public string PromptId { get; set; }
    }
}