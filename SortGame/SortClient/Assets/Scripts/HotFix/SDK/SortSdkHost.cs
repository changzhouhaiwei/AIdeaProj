using System;
using EwingSoft.GameSDK;
using UnityEngine;

/// <summary>
/// Android SDK 启动挂点。
/// 原生 UnitySendMessage 打到 UnityCallbackDelegate 和 GPSDKMgr，名字不能改。
/// </summary>
public class SortSdkHost : MonoBehaviour
{
    const string CallbackObjectName = "UnityCallbackDelegate";

    public static event Action<string> OnAndroidMessage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureCallbackListener();
        if (FindObjectOfType<SortSdkHost>() != null)
        {
            return;
        }

        var host = new GameObject("GPSDKMgr");
        host.AddComponent<SortSdkHost>();
        DontDestroyOnLoad(host);
    }

    static void EnsureCallbackListener()
    {
        var existing = FindObjectOfType<GameSDKCallbackListener>();
        if (existing != null)
        {
            existing.gameObject.name = CallbackObjectName;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        var go = new GameObject(CallbackObjectName);
        go.AddComponent<GameSDKCallbackListener>();
        DontDestroyOnLoad(go);
    }

    /// <summary>卸载挽留快捷方式从 Android 打过来的消息。</summary>
    public void OnRecieveAndroidMsg(string json)
    {
        Debug.Log("[SortSdk] " + json);
        OnAndroidMessage?.Invoke(json);
    }
}
