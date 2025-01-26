using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{

    /// <summary>
    /// Class representing progress data.
    /// </summary>
    public class ProgressData
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("max")]
        public int Max { get; set; }

        [JsonProperty("prompt_id")]
        public string PromptId { get; set; }

        [JsonProperty("node")]
        public string Node { get; set; }
    }
}