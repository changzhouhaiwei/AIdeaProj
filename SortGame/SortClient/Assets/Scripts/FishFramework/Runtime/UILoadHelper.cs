using System.Collections.Generic;
using UnityEngine;
using xasset;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FishFramework
{
    public class UILoadHelper : MonoBehaviour
    {
        private static readonly Dictionary<string, Asset> cacheAssets = new();

        public static T LoadAsset<T>(string assetPath) where T : Object
        {
            if (cacheAssets.TryGetValue(assetPath, out Asset value))
            {
                return value.Get<T>();
            }

            Asset asset = ResourceModule.LoadAsset(assetPath, typeof(T));
            if (asset == null)
            {
#if UNITY_EDITOR
                // Level Editor Test Level may run before xasset Versions is fully ready.
                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
                return null;
#endif
            }

            cacheAssets.Add(assetPath, asset);
            return asset.asset as T;
        }

        public static void RemoveAsset(string assetPath)
        {
            if (cacheAssets.TryGetValue(assetPath, out Asset value))
            {
                value.ReleaseRef();
                cacheAssets.Remove(assetPath);
            }
        }
    }
}