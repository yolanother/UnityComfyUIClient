using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{

    public class ExecutionInfo
    {
        [JsonProperty("queue_remaining")]
        public int QueueRemaining { get; set; }
    }
}