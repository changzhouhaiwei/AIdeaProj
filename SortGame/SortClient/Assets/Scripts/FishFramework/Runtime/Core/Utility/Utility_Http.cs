using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FishFramework
{
    public static partial class Utility
    {
        public static void RequestByJsonBodyPost(string url, string json, Action<bool, string> func)
        {
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            DownloadHandler downloadHandler = new DownloadHandlerBuffer();
            request.downloadHandler = downloadHandler;
            request.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SendWebRequest().completed += operation =>
            {
                if (!string.IsNullOrEmpty(request.error))
                {
                    Debug.LogWarning(request.error);
                    func(false, request.error);
                }
                else
                {
                    func(true, request.downloadHandler.text);
                }

                request.Dispose();
            };
        }
    }
}