using System;
using System.Collections.Generic;
using I2.Loc;
using UnityEngine;
using xasset;
using FishFramework;
using Object = UnityEngine.Object;

public class I2LocalizeMgr : MonoSingleton<I2LocalizeMgr>, IResourceManager_Bundles
{
    private readonly Dictionary<string, Asset> cacheAssets = new();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public Object LoadFromBundle(string path, Type assetType)
    {
        // load from a bundle using path and return the object
        Debug.Log($"path ----- {path} -- {assetType}");
        var asset = ResourceModule.LoadAsset(path, assetType);
        if (asset == null)
        {
            return null;
        }

        cacheAssets.Add(path, asset);
        return asset.asset;
    }

    private void CleanCacheAssets()
    {
        foreach (var asset in cacheAssets.Values)
        {
            asset.ReleaseRef();
        }

        cacheAssets.Clear();
    }

    public void UpdateSources()
    {
        CleanCacheAssets();
        ResourceManager.pInstance.CleanResourceCache();
        if (!ResourceManager.pInstance.mBundleManagers.Contains(this))
        {
            ResourceManager.pInstance.mBundleManagers.Add(this);
        }

        if (LocalizationManager.UpdateSources())
        {
            ResourceManager.pInstance.mBundleManagers.Remove(this);
        }
    }

    public static void SetLanguage(string language)
    {
        LocalizationFontSwitcher.EnsureListening();
        LocalizationManager.CurrentLanguage = language;
        LocalizationFontSwitcher.ApplyRegistered();
    }

    public static string GetLanguage()
    {
        return LocalizationManager.CurrentLanguage;
    }

    public static string GetString(string term)
    {
        string str = LocalizationManager.GetTranslation(term);
        if (string.IsNullOrEmpty(str))
        {
            return term;
        }

        return str;
    }
}