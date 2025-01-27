using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DoubTech.ComfyUI.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace DoubTech.ComfyUI
{
    public class ComfyUIPrompt : MonoBehaviour
    {
        [Header("Base Configuration")]
        [SerializeField] private TextAsset promptTemplate;
        [SerializeField] private BaseComfyUIRequest comfyUI;
        
        [Header("Seeding")]
        [SerializeField] private bool randomSeed;
        [SerializeField] private long seed;
        
        private Dictionary<string, object> variables = new(); 
        
        public object this[string key]
        {
            get => variables.TryGetValue(key, out var v) ? v : null;
            set => variables[key] = value;
        }

        public void GenerateSeed()
        {
            seed = Random.Range(0, int.MaxValue);
        }

        public void SubmitPrompt()
        {
            if (!OnValidateSubmit()) return;
            var prompt = OnPreparePrompt(promptTemplate.text);
            if (!OnValidatePrompt(prompt)) return;
            comfyUI.SubmitPrompt(prompt);
        }

        protected virtual bool OnValidateSubmit()
        {
            return true;
        }

        protected virtual bool OnValidatePrompt(string prompt)
        {
            return true;
        }

        protected virtual string OnPreparePrompt(string prompt)
        {
            if (randomSeed)
            {
                GenerateSeed();
            }
            
            // Replace any variable that might be in the prompt with the value including variables with default values $(variable_name:optional_default_value) or $(variable_name) replacing all with the value using regex
            
            this["seed"] = seed;
            foreach (var variable in variables)
            {
                var value = variable.Value;
                string stringValue = "";
                if (value is Texture2D texture)
                {
                    // Create a base64 string data url
                    stringValue = "data:image/png;base64," + System.Convert.ToBase64String(texture.EncodeToPNG());
                }
                else if (value is RawImage image)
                {
                    // If image.texture is a render texture
                    if (image.texture is RenderTexture renderTexture)
                    {
                        // Create a temporary texture
                        var tempTexture = new Texture2D(renderTexture.width, renderTexture.height);
                        // Copy the render texture to the temporary texture
                        RenderTexture.active = renderTexture;
                        tempTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                        tempTexture.Apply();
                        // Create a base64 string data url
                        stringValue = "data:image/png;base64," + System.Convert.ToBase64String(tempTexture.EncodeToPNG());
                    }
                    else
                    {
                        // Create a base64 string data url
                        stringValue = "data:image/png;base64," + System.Convert.ToBase64String(((Texture2D) image.texture).EncodeToPNG());
                    }
                }
                else if(value is string str)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        // Trim end spaces and leading/trailing "
                        str = str.Trim();
                        str = str.Trim('"');
                        var jsonData = new StringData { data = str };
                        // Use jsonconvert to create a single line json string
                        stringValue = JsonConvert.SerializeObject(jsonData);
                        stringValue = stringValue[9..^4];
                    }
                }
                else if(null != value)
                {
                    stringValue = value.ToString();
                }
                
                prompt = prompt.Replace($"{{{variable.Key}}}", stringValue);
                
                var varRegex = new Regex(@"\$\(" + variable.Key + @"(:([^)]+))?\)", RegexOptions.Compiled);
                // Replace the matches with the value
                prompt = varRegex.Replace(prompt, stringValue);
            }
            var regex = new Regex(@"\$\(([^:)]+):([^)]+)\)", RegexOptions.Compiled);
            // Find variables in the prompt that aren't set and replace with any default values syntax is $(variable:default)
            var matches = regex.Matches(prompt);
            foreach (Match match in matches)
            {
                var v = match.Groups[1].Value;
                var defaultValue = match.Groups[2].Value;
                prompt = prompt.Replace(match.Value, defaultValue);
            }

            return prompt;
        }
    }
    
    // Editor to add, remove and edit variables and submit the prompt
    #if UNITY_EDITOR
    [CustomEditor(typeof(ComfyUIPrompt))]
    public class ComfyUIPromptEditor : Editor
    {
        private ComfyUIPrompt script;
        private TextAsset promptAsset;
        private List<KeyValuePair<string, Type>> variableNames = new();
        private int newVariableType;
        private string newVariableName;

        public override void OnInspectorGUI()
        {
            script = (ComfyUIPrompt)target;

            DrawDefaultInspector();
            // Show a list of variables with a button to add a new one
            foreach (var variable in variableNames)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(variable.Key);

                if (variable.Value == typeof(Texture2D))
                {
                    var texture = (Texture2D)script[variable.Key];
                    var newTexture = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false);
                    if (newTexture != texture)
                    {
                        script[variable.Key] = newTexture;
                    }
                }
                else if (variable.Value == typeof(RawImage))
                {
                    var image = (RawImage)script[variable.Key];
                    var newImage = (RawImage)EditorGUILayout.ObjectField(image, typeof(RawImage), true);
                    if (newImage != image)
                    {
                        script[variable.Key] = newImage;
                    }
                }
                else
                {
                    var newValue = EditorGUILayout.TextField(script[variable.Key]?.ToString());
                    if (newValue != script[variable.Key]?.ToString())
                    {
                        script[variable.Key] = newValue;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            // Display a type dropdown with an add button next to it with string or image types
            EditorGUILayout.BeginHorizontal();
            newVariableName = EditorGUILayout.TextField("New Variable Name", newVariableName);
            var types = new[] { "Value", "Texture", "Image" };
            newVariableType = EditorGUILayout.Popup(newVariableType, types);
            if (GUILayout.Button("Add"))
            {
                Type type = typeof(string);
                if (newVariableType == 1)
                {
                    type = typeof(Texture2D);
                }
                else if (newVariableType == 2)
                {
                    type = typeof(RawImage);
                }

                variableNames.Add(new KeyValuePair<string, Type>(newVariableName, type));
            }
            GUILayout.EndHorizontal();

            // Add a button to submit the prompt
            if (GUILayout.Button("Submit Prompt"))
            {
                script.SubmitPrompt();
            }
        }
    }
    
    #endif
}