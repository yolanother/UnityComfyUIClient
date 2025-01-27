using System.Collections.Generic;
using System.Threading.Tasks;
using DoubTech.ComfyUI.Data;
using UnityEngine;
using UnityEngine.Events;

namespace DoubTech.ComfyUI
{
    public abstract class BaseComfyUIRequest : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private ComfyUIConfig config = new ComfyUIConfig();
        
        [Header("Events")]
        [SerializeField] private UnityEvent<string> onProgressMessage = new UnityEvent<string>();
        [SerializeField] private UnityEvent<float> onProgress = new UnityEvent<float>();
        [SerializeField] private UnityEvent<Texture2D> onImageReceived = new UnityEvent<Texture2D>();
        
        private List<ComfyUIRequest> _requests = new List<ComfyUIRequest>();
        
        protected virtual Texture2D OnGetTargetTexture()
        {
            return null;
        }

        protected virtual async Task OnProgress(ProgressData progressData)
        {
            onProgressMessage?.Invoke($"Processing {progressData.NodeName}...");
            onProgress?.Invoke((float) progressData.Value / (float) progressData.Max);
        }

        protected virtual void Update()
        {
            for (int i = 0; i < _requests.Count; i++)
            {
                var request = _requests[i];
                if (!request.DispatchMessageQueue())
                {
                    _requests.RemoveAt(i);
                    i--;
                }
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
            onProgressMessage?.Invoke("Processing prompt...");
            try
            {
                var request = new ComfyUIRequest(config, promptText)
                {
                    OnGetTargetTexture = OnGetTargetTexture,
                    OnProgress = OnProgress,
                    OnImageReceived = onImageReceived.Invoke
                };
                _requests.Add(request);
                await request.Submit();
                _requests.Remove(request);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                onProgressMessage?.Invoke("Error processing prompt: " + e.Message);
            }
            finally
            {
                onProgressMessage?.Invoke("");
            }
        }

        /// <summary>
        /// Ensures WebSocket cleanup on destroy.
        /// </summary>
        protected virtual void OnDestroy()
        {
            foreach (var request in _requests)
            {
                request.Close();
            }
        }
    }
}