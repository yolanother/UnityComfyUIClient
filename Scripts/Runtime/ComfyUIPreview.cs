using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using DoubTech.ComfyUI.Data;
using UnityEngine;
using UnityEngine.UI;
using DoubTech.ComfyUI.Websocket.NativeWebSocket;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine.Events;

namespace DoubTech.ComfyUI
{
    public class ComfyUIPreview : BaseComfyUIRequest
    {
        [Header("UI Elements")]
        [Tooltip("RawImage component to display the textures")]
        public RawImage rawImage;

        private Texture2D currentTexture;

        private void Start()
        {
            // Create a 1024x1024 texture and fill it with black
            currentTexture = new Texture2D(rawImage.texture.width, rawImage.texture.height);
            currentTexture.SetPixels(new Color[currentTexture.width * currentTexture.height]);
            currentTexture.Apply();
        }

        protected override Texture2D OnGetTargetTexture()
        {
            return (Texture2D)rawImage?.texture;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ComfyUIPreview))]
    public class ComfyUIPreviewEditor : Editor
    {
        private bool showDefaultInspector = false;
        private string customPrompt = string.Empty;
        private TextAsset promptAsset;

        public override void OnInspectorGUI()
        {
            ComfyUIPreview script = (ComfyUIPreview)target;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Custom Prompt", EditorStyles.boldLabel);
            promptAsset = (TextAsset)EditorGUILayout.ObjectField("Prompt Asset", promptAsset, typeof(TextAsset), false);

            if (!promptAsset)
            {
                customPrompt = EditorGUILayout.TextArea(customPrompt, GUILayout.Height(100));
            }

            if (GUILayout.Button("Submit Custom Prompt"))
            {
                if(promptAsset) script.SubmitPrompt(promptAsset);
                else script.SubmitPrompt(customPrompt);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Clear Texture"))
            {
                script.rawImage.texture = null;
            }

            EditorGUILayout.Space();
            showDefaultInspector = EditorGUILayout.Foldout(showDefaultInspector, "Show Default Inspector");

            if (showDefaultInspector)
            {
                DrawDefaultInspector();
            }
        }
    }
#endif
}
