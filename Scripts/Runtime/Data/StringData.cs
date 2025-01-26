using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{
    /// <summary>
    /// Simple string data
    /// </summary>
    public class StringData
    {
        [JsonProperty("data")]
        public string data;
    }
}