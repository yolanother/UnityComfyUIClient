using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DoubTech.ComfyUI.Data;
using DoubTech.ComfyUI.Websocket.NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace DoubTech.ComfyUI
{
    public abstract class BaseComfyUIRequest : MonoBehaviour
    {

        [Header("Connection Settings")]
        [Tooltip("Host address for the WebSocket and HTTP server")]
        [SerializeField]
        private string host = "127.0.0.1";

        [Tooltip("Port number for the WebSocket and HTTP server")]
        [SerializeField]
        private int port = 8188;

        [Tooltip("Client ID for WebSocket communication")]
        public string clientId;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<float> onProgress = new UnityEvent<float>();
        [SerializeField] private UnityEvent<Texture2D> onImageReceived = new UnityEvent<Texture2D>();
        
        private WebSocket websocket;
        private string _connectionId = Guid.NewGuid().ToString();

        private string WebsocketUrl => $"ws://{host}:{port}/ws";
        private string HttpServerUrl => $"http://{host}:{port}";
        private string ClientId => $"{_connectionId}::{clientId}";

        /// <summary>
        /// Handles incoming WebSocket messages.
        /// Updates the RawImage with the received texture data.
        /// </summary>
        /// <param name="message">Received message in bytes.</param>
        protected virtual void OnWebSocketMessage(byte[] messageBytes)
        {
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Debug.Log($"Received message: {messageString}");
            MessageBase message = JsonConvert.DeserializeObject<MessageBase>(messageString);

            switch (message.Type)
            {
                case "executed":
                    ExecutionData executedData = JsonConvert.DeserializeObject<ExecutionData>(message.Data.ToString());
                    _ = OnExecuted(executedData);
                    break;
                case "progress":
                    ProgressData progressData = JsonConvert.DeserializeObject<ProgressData>(message.Data.ToString());
                    _ = OnProgress(progressData);
                    break;
                case "status":
                    StatusData statusData = JsonConvert.DeserializeObject<StatusData>(message.Data.ToString());
                    _ = OnStatus(statusData);
                    break;
                case "execution_success":
                    ExecutionSuccessData successData = JsonConvert.DeserializeObject<ExecutionSuccessData>(message.Data.ToString());
                    _ = OnExecutionSuccess(successData);
                    break;
            }
        }
        
        protected virtual Texture2D OnGetTargetTexture()
        {
            return null;
        }

        protected virtual async Task OnExecuted(ExecutionData executedData)
        {
            if(null == executedData.Output) return;
            if(null == executedData.Output.Images) return;
            
            foreach (var image in executedData.Output.Images)
            {
                Debug.Log($"Downloading image: {image.Filename}");
                var texture = await GetImageAsync(image.Filename, image.Subfolder, image.Type, OnGetTargetTexture());
                onImageReceived?.Invoke(texture);
            }
        }

        protected virtual async Task OnProgress(ProgressData progressData)
        {
            onProgress?.Invoke((float) progressData.Value / (float) progressData.Max);
        }

        protected virtual async Task OnStatus(StatusData statusData)
        {
            
        }

        protected virtual async Task OnExecutionSuccess(ExecutionSuccessData successData)
        {
            
        }

        protected virtual void Update()
        {
            websocket?.DispatchMessageQueue();
        }

        public void Close()
        {
            if (websocket != null)
            {
                _ = websocket.Close();
            }
            websocket = null;
        }

        public void Connect()
        {
            _ = ConnectAsync();
        }

        /// <summary>
        /// Connects to the WebSocket server if not already connected.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (websocket == null || websocket.State != WebSocketState.Open)
            {
                if (websocket != null)
                {
                    await websocket.Close();
                }

                websocket = new WebSocket($"{WebsocketUrl}?clientId={ClientId}");

                websocket.OnOpen += () => Debug.Log("WebSocket Opened");
                websocket.OnMessage += OnWebSocketMessage;
                websocket.OnError += (error) => Debug.LogError($"WebSocket Error: {error}");
                websocket.OnClose += (code) => Debug.Log($"WebSocket Closed: {code}");

                await websocket.Connect();
            }
        }

        public void SubmitPrompt(TextAsset prompt)
        {
            _ = PromptAsync(prompt.text);
        }

        public void SubmitPrompt(string text)
        {
            _ = PromptAsync(text);
        }

        /// <summary>
        /// Submits a prompt text via an HTTP POST request.
        /// Establishes WebSocket connection if necessary.
        /// </summary>
        /// <param name="promptText">The prompt text to submit.</param>
        public async Task PromptAsync(string promptText)
        {
            _ = ConnectAsync();

            using (HttpClient client = new HttpClient())
            {
                var text = "{\"prompt\":" + promptText + ",\"client_id\":\"" + ClientId + "\"}";
                var content = new StringContent(text, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync($"{HttpServerUrl}/prompt", content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Prompt Response: {responseBody}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send prompt: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Fetches an image by filename, subfolder, and folder type via an HTTP GET request.
        /// </summary>
        public async Task<Texture2D> GetImageAsync(string filename, string subfolder, string folderType, Texture2D texture = null)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string query = $"filename={filename}&subfolder={subfolder}&type={folderType}";
                    string url = $"{HttpServerUrl}/view?{query}";

                    byte[] imageBytes = await client.GetByteArrayAsync(url);
                    if(!texture) texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        return texture;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to fetch image: {ex.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Fetches multiple images from a history endpoint by prompt ID via an HTTP GET request.
        /// </summary>
        public async Task<Dictionary<string, List<Texture2D>>> GetImagesAsync(string promptId)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"{HttpServerUrl}/history/{promptId}";
                    string response = await client.GetStringAsync(url);
                    var history = JsonUtility.FromJson<Dictionary<string, object>>(response);
                    var outputImages = new Dictionary<string, List<Texture2D>>();

                    if (history.TryGetValue(promptId, out object promptHistory))
                    {
                        var outputs = promptHistory as Dictionary<string, object>;
                        foreach (var nodeId in outputs.Keys)
                        {
                            var nodeOutput = outputs[nodeId] as Dictionary<string, object>;
                            var textures = new List<Texture2D>();
                            if (nodeOutput.TryGetValue("images", out object images))
                            {
                                foreach (var image in images as List<Dictionary<string, string>>)
                                {
                                    string filename = image["filename"];
                                    string subfolder = image["subfolder"];
                                    string folderType = image["type"];
                                    Texture2D texture = await GetImageAsync(filename, subfolder, folderType);
                                    if (texture != null)
                                    {
                                        textures.Add(texture);
                                    }
                                }
                            }
                            outputImages[nodeId] = textures;
                        }
                    }
                    return outputImages;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to fetch images: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Updates the host and port properties. Disconnects if already connected.
        /// </summary>
        public string Host
        {
            get => host;
            set
            {
                if (host != value)
                {
                    host = value;
                    if (websocket != null && websocket.State == WebSocketState.Open)
                    {
                        _ = websocket.Close();
                        websocket = null;
                    }
                }
            }
        }

        public int Port
        {
            get => port;
            set
            {
                if (port != value)
                {
                    port = value;
                    if (websocket != null && websocket.State == WebSocketState.Open)
                    {
                        _ = websocket.Close();
                        websocket = null;
                    }
                }
            }
        }

        /// <summary>
        /// Ensures WebSocket cleanup on destroy.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (websocket != null)
            {
                _ = websocket.Close(); 
            }
        }
    }
}