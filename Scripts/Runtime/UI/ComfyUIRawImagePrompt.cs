using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoubTech.ComfyUI.UI
{
    public class ComfyUIRawImagePrompt : ComfyUITMProPrompt
    {
        [SerializeField] private RawImage image;

        protected override string OnPreparePrompt(string prompt)
        {
            this["image"] = image;
            return base.OnPreparePrompt(prompt);
        }
    }
}