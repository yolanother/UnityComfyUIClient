using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{

    /// <summary>
    /// Class representing status data.
    /// </summary>
    public class StatusData
    {
        [JsonProperty("status")]
        public ExecutionStatus Status { get; set; }

        [JsonProperty("sid")]
        public string Sid { get; set; }
    }
}