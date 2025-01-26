using Newtonsoft.Json;
using System.Collections.Generic;

namespace DoubTech.ComfyUI.Data
{
    /// <summary>
    /// Base class representing a generic response.
    /// </summary>
    public class MessageBase
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
