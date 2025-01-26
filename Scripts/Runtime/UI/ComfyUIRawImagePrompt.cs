using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoubTech.ComfyUI.UI
{
    public class ComfyUIRawImagePrompt : ComfyUIPrompt
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField prompt;
        [SerializeField] private RawImage image;

        private void Start()
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
            this["image"] = image;
            PlayerPrefs.SetString("ComfyUI/LastImagePrompt", this.prompt.text);
            return base.OnPreparePrompt(prompt);
        }
    }
}