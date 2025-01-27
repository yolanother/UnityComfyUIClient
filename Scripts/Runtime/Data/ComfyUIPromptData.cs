using Newtonsoft.Json;
using System.Collections.Generic;

namespace DoubTech.ComfyUI.Data
{
    public class PromptInputDataset
    {
        /// <summary>
        /// Dictionary representing dynamic inputs for the dataset.
        /// </summary>
        [JsonProperty("inputs")]
        public Dictionary<string, object> Inputs { get; set; }

        /// <summary>
        /// The class type of the dataset.
        /// </summary>
        [JsonProperty("class_type")]
        public string ClassType { get; set; }

        /// <summary>
        /// Metadata associated with the dataset.
        /// </summary>
        [JsonProperty("_meta")]
        public Meta Meta { get; set; }

        /// <summary>
        /// Optional property indicating if the dataset has changed.
        /// </summary>
        [JsonProperty("is_changed", NullValueHandling = NullValueHandling.Ignore)]
        public List<bool> IsChanged { get; set; }
    }

    public class Meta
    {
        /// <summary>
        /// Title of the dataset.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class ComfyUIPromptData
    {
        /// <summary>
        /// Dictionary representing datasets, keyed by their identifier.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, PromptInputDataset> Datasets { get; set; }
    }
}