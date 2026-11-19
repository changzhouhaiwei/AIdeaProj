using System.Collections.Generic;
using cfg;
using SimpleJSON;
using UnityEngine;
using xasset;
using FishFramework;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ConfigHelper
{
    public static Tables Tables { get; private set; }

    // public static Dictionary<int, List<CharacterInfoConfig>> CharacterBaseIdWithInfoCfgDic { get; } = new();

    public static void Initialize()
    {
        Tables = new Tables(Loader);
        // MakeCharacterInfoConfig();
    }

    /// <summary>
    /// Level Editor Test Level skips GameApp — ensure XSort skin configs exist before LoadLevel.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (Tables != null)
        {
            return;
        }

        Initialize();
    }

    private static JSONNode Loader(string fileName)
    {
        string assetPath = $"Assets/CABRes/Configs/{fileName}.json";
        TextAsset textAsset = null;

#if UNITY_EDITOR
        // Editor 直接读源文件，避免 xasset 仿真缓存到旧表后 Luban 反序列化失败、LoginUI 永远打不开。
        textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
#endif
        if (textAsset == null)
        {
            textAsset = ResourceModule.LoadAsset<TextAsset>(assetPath);
        }

        if (null == textAsset)
        {
            Debug.LogError($"ConfigHelper: {fileName} missing.");
            return new JSONArray();
        }

        JSONNode json = JSON.Parse(System.Text.Encoding.UTF8.GetString(textAsset.bytes));
        if (json == null || json.IsNull)
        {
            Debug.LogError($"ConfigHelper: {fileName} parse failed.");
            return new JSONArray();
        }

        return json;
    }

}