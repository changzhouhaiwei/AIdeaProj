using System;
using UnityEngine;

[Serializable]
public class WallpaperConfigData
{
    public float speed = 48f;
    public int fps = 30;
    public bool mute = true;
    /// <summary>远景相对「贴齐屏幕高度」的倍数；&lt;1 缩小，&gt;1 放大。</summary>
    public float backgroundScale = 1f;
}

/// <summary>
/// Loads optional config TextAsset from Resources/Wallpaper/config.
/// Scene fields on WallpaperApp take priority.
/// </summary>
public static class WallpaperConfig
{
    const string ResourcePath = "Wallpaper/config";

    public static WallpaperConfigData Load(TextAsset overrideAsset = null)
    {
        var cfg = new WallpaperConfigData();
        var asset = overrideAsset != null ? overrideAsset : Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
            return cfg;
        try
        {
            JsonUtility.FromJsonOverwrite(asset.text, cfg);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WallpaperConfig] parse failed: {e.Message}");
        }
        return cfg;
    }
}
