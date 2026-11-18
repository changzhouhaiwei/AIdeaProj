using System;
using UnityEngine;
using FishFramework;

public class SplashController : MonoBehaviour
{
    private void Start()
    {
        Utility.DGDelayedCall(0.1f, () =>
        {
            HideSplashScreen();
        });
    }

    private void HideSplashScreen()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("hideSplashScreen");
        }
#endif
    }

}