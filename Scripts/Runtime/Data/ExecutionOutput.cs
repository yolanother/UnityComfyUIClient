using System.Collections.Generic;
using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{

    public class ExecutionOutput
    {
        [JsonProperty("images")]
        public List<ExecutionImage> Images { get; set; }
    }
}