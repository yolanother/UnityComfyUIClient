using Newtonsoft.Json;

namespace DoubTech.ComfyUI.Data
{

    public class ExecutionStatus
    {
        [JsonProperty("exec_info")]
        public ExecutionInfo ExecInfo { get; set; }
    }
}