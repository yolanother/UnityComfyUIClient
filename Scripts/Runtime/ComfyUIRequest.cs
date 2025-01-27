using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DoubTech.ComfyUI.Data;
using DoubTech.ComfyUI.Lib.JsonLib;
using DoubTech.ComfyUI.Websocket.NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace DoubTech.ComfyUI
{
    [Serializable]
    public class ComfyUIConfig
    {
        [Header("Connection Settings")]
        [Tooltip("Host address for the WebSocket and HTTP server")]
        [SerializeField]
        public string host = "127.0.0.1";

        [Tooltip("Port number for the WebSocket and HTTP server")]
        [SerializeField]
        public int port = 8188;

        [Tooltip("Client ID for WebSocket communication")]
        public string clientId;

        public string WebsocketUrl => $"ws://{host}:{port}/ws";
        public string HttpServerUrl => $"http://{host}:{port}";
    }
    
    public class ComfyUIRequest
    {
        private ComfyUIConfig _config;
        private string _requestId;

        private WebSocket _websocket;
        private readonly string _prompt;
        private readonly JSONNode _promptData;
        
        public JSONNode PromptData => _promptData;

        public Func<ExecutionData, Task> OnExecuted;
        public Func<ProgressData, Task> OnProgress;
        public Func<StatusData, Task> OnStatus;
        public Func<ExecutionSuccessData, Task> OnExecutionSuccess;
        public Func<Texture2D> OnGetTargetTexture;
        public Action<Texture2D> OnImageReceived;
        private TaskCompletionSource<bool> _activeTask;

        private string ClientId => $"{_config.clientId}::{_requestId}";

        public ComfyUIRequest(ComfyUIConfig config, string prompt)
        {
            _config = config;
            _requestId = Guid.NewGuid().ToString();
            _prompt = prompt;
            try
            {
                _promptData = JSONNode.Parse(prompt);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse prompt: {e.Message}");
                _promptData = new JSONObject();
            }

            _websocket = new WebSocket($"{_config.WebsocketUrl}?clientId={ClientId}");
            _websocket.OnOpen += () => Debug.Log("WebSocket Opened");
            _websocket.OnMessage += OnWebSocketMessage;
            _websocket.OnError += (error) => Debug.LogError($"WebSocket Error: {error}");
            _websocket.OnClose += (code) => Debug.Log($"WebSocket Closed: {code}");
        }

        public async Task<bool> Submit()
        {
            if (_activeTask != null && !_activeTask.Task.IsCompleted) return await _activeTask.Task;
            _ = _websocket.Connect();

            _activeTask = new TaskCompletionSource<bool>();
            using (HttpClient client = new HttpClient())
            {
                var text = "{\"prompt\":" + _prompt + ",\"client_id\":\"" + ClientId + "\"}";
                var content = new StringContent(text, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync($"{_config.HttpServerUrl}/prompt", content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Prompt Response: {responseBody}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send prompt: {ex.Message}");
                    _ = _websocket.Close();
                    _activeTask.SetResult(false);
                }
            }

            return await _activeTask.Task;
        }

        public bool DispatchMessageQueue()
        {
            if(null == _websocket) return false;
            _websocket?.DispatchMessageQueue();
            return true;
        }
        
        /// <summary>
        /// Handles incoming WebSocket messages.
        /// Updates the RawImage with the received texture data.
        /// </summary>
        /// <param name="message">Received message in bytes.</param>
        protected virtual void OnWebSocketMessage(byte[] messageBytes)
        {
            string messageString = Encoding.UTF8.GetString(messageBytes);
            if (!messageString.Trim().StartsWith('{'))
            {
                // Assume this might be binary image data and try to parse it
                var texture = OnGetTargetTexture?.Invoke();
                if(!texture) texture = new Texture2D(2, 2);
                if (texture.LoadImage(messageBytes))
                {
                    OnImageReceived?.Invoke(texture);
                }
                else
                {
                    // quick fill the texture with black
                    var pixels = new Color[texture.width * texture.height];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = Color.black;
                    }
                    texture.SetPixels(pixels);
                }

                return;
            }
            Debug.Log($"Received message: {messageString}");
            MessageBase message = JsonConvert.DeserializeObject<MessageBase>(messageString);

            switch (message.Type)
            {
                case "executed":
                    ExecutionData executedData = JsonConvert.DeserializeObject<ExecutionData>(message.Data.ToString());
                    _ = HandleOnExecuted(executedData);
                    break;
                case "progress":
                    ProgressData progressData = JsonConvert.DeserializeObject<ProgressData>(message.Data.ToString());
                    _ = HandleOnProgress(progressData);
                    break;
                case "status":
                    StatusData statusData = JsonConvert.DeserializeObject<StatusData>(message.Data.ToString());
                    _ = HandleOnStatus(statusData);
                    break;
                case "execution_success":
                    ExecutionSuccessData successData = JsonConvert.DeserializeObject<ExecutionSuccessData>(message.Data.ToString());
                    _ = HandleOnExecutionSuccess(successData);
                    break;
            }
        }

        private async Task HandleOnExecuted(ExecutionData executedData)
        {
            if(null != OnExecuted) await OnExecuted.Invoke(executedData);
            if(null == executedData.Output) return;
            if(null == executedData.Output.Images) return;
            
            foreach (var image in executedData.Output.Images)
            {
                Debug.Log($"Downloading image: {image.Filename}");
                var texture = await GetImageAsync(image.Filename, image.Subfolder, image.Type, OnGetTargetTexture());
                OnImageReceived?.Invoke(texture);
            }
        }

        private async Task HandleOnProgress(ProgressData progressData)
        {
            var nodeId = progressData.Node;
            progressData.NodeName = PromptData[nodeId]["_meta"]["title"];
            if (string.IsNullOrEmpty(progressData.NodeName))
            {
                progressData.NodeName = "Node " + nodeId;
            }
            if(null != OnProgress) await OnProgress.Invoke(progressData);
        }

        private async Task HandleOnStatus(StatusData statusData)
        {
            if(null != OnStatus) await OnStatus.Invoke(statusData);
        }

        private async Task HandleOnExecutionSuccess(ExecutionSuccessData successData)
        {
            if(null != OnExecutionSuccess) await OnExecutionSuccess.Invoke(successData);
            _activeTask?.SetResult(true);
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
                    string url = $"{_config.HttpServerUrl}/view?{query}";

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
                    string url = $"{_config.HttpServerUrl}/history/{promptId}";
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

        public void Close()
        {
            if (_websocket != null)
            {
                _ = _websocket.Close();
            }
            _websocket = null;
        }

        /// <summary>
        /// Updates the host and port properties. Disconnects if already connected.
        /// </summary>
        public string Host
        {
            get => _config.host;
            set
            {
                if (_config.host != value)
                {
                    _config.host = value;
                    if (_websocket != null && _websocket.State == WebSocketState.Open)
                    {
                        _ = _websocket.Close();
                        _websocket = null;
                    }
                }
            }
        }

        public int Port
        {
            get => _config.port;
            set
            {
                if (_config.port != value)
                {
                    _config.port = value;
                    if (_websocket != null && _websocket.State == WebSocketState.Open)
                    {
                        _ = _websocket.Close();
                        _websocket = null;
                    }
                }
            }
        }
    }
}