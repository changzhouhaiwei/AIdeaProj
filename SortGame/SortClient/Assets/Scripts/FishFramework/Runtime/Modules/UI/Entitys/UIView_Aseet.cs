using UnityEngine;
using xasset;

namespace FishFramework
{
    public abstract partial class UIView
    {
        #region 资源管理

        protected static T LoadAsset<T>(string assetPath) where T : Object
        {
            if (cacheAssets.TryGetValue(assetPath, out Asset value))
            {
                return value.Get<T>();
            }

            Asset request = ResourceModule.LoadAsset(assetPath, typeof(T));
            if (request == null)
            {
                return null;
            }

            cacheAssets.Add(assetPath, request);
            return request.asset as T;
        }

        protected static Sprite LoadSprite(string abDirectory, string assetName)
        {
            string assetNamePath = $"Assets/CABRes/Sprites/{abDirectory}/{assetName}.png";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static Sprite LoadSpriteBg(string abDirectory, string assetName)
        {
            string assetNamePath = $"Assets/CABRes/Sprites/{abDirectory}/{assetName}.jpg";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static Sprite LoadSmallSprite(string assetName)
        {
            string assetNamePath = $"Assets/CABRes/Sprites/ItemSmall/{assetName}.png";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static Sprite LoadAtlasSprite(string abDirectory, string assetName)
        {
            string assetNamePath = $"Assets/CABRes/SpriteAtlas/{abDirectory}/{assetName}.png";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static AudioClip LoadMusic(string assetName)
        {
            string assetNamePath = $"Assets/CABRes/Audio/Music/{assetName}.wav";
            return LoadAsset<AudioClip>(assetNamePath);
        }

        protected static AudioClip LoadSFX(string assetName)
        {
            string assetNamePath = $"Assets/CABRes/Audio/SFX/{assetName}.wav";
            return LoadAsset<AudioClip>(assetNamePath);
        }

        private static void Clear()
        {
            foreach (Asset request in cacheAssets.Values)
            {
                request.ReleaseRef();
            }

            cacheAssets.Clear();
        }

        #endregion 资源管理
    }
}