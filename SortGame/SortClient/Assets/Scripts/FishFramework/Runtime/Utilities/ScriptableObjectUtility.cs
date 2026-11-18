#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScriptableObjectUtility
{
    public static T[] FindAssets<T>() where T : ScriptableObject
    {
        var builds = new List<T>();
        var guilds = AssetDatabase.FindAssets("t:" + typeof(T).FullName);
        foreach (var guild in guilds)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guild);
            if (string.IsNullOrEmpty(assetPath))
            {
                continue;
            }

            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                continue;
            }

            builds.Add(asset);
        }

        return builds.ToArray();
    }

    public static T FindOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var guilds = AssetDatabase.FindAssets($"t:{typeof(T).FullName}");
        foreach (var guild in guilds)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guild);
            if (string.IsNullOrEmpty(assetPath))
            {
                continue;
            }

            var asset = GetOrCreateAsset<T>(assetPath);
            if (asset == null)
            {
                continue;
            }

            return asset;
        }

        return GetOrCreateAsset<T>(path);
    }

    public static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        CreateDirectoryIfNecessary(path);
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void CreateDirectoryIfNecessary(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir) || Directory.Exists(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
    }
}
#endif