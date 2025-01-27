using TMPro;
using UnityEngine;

namespace DoubTech.ComfyUI.UI
{
    public class ComfyUITMProPrompt : ComfyUIPrompt
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField prompt;

        protected virtual void Start()
        {
            prompt.text = PlayerPrefs.GetString("ComfyUI/LastImagePrompt", "");
        }
        
        protected override bool OnValidateSubmit()
        {
            return !string.IsNullOrEmpty(prompt.text);
        }

        protected override string OnPreparePrompt(string prompt)
        {
            this["prompt"] = this.prompt.text;
            PlayerPrefs.SetString("ComfyUI/LastImagePrompt", this.prompt.text);
            return base.OnPreparePrompt(prompt);
        }
    }
}