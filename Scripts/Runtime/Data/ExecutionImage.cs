using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{

    public class ExecutionImage
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("subfolder")]
        public string Subfolder { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}