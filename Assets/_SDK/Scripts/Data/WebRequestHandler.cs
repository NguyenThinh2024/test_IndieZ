using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Thinh.Base.Data
{
    public class WebRequestHandler : MonoSingleton<WebRequestHandler>
    {
        public async Task<string> GetRequest(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                var operation = webRequest.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {webRequest.error}");
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }

        public async Task<string> PostRequest(string uri, string jsonData)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(uri, "POST"))
            {
                byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                var operation = webRequest.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {webRequest.error}");
                    if (webRequest.downloadHandler != null) return webRequest.downloadHandler.text;
                    return null;
                }
                else
                {
                    return webRequest.downloadHandler.text;
                }
            }
        }
    }
}

