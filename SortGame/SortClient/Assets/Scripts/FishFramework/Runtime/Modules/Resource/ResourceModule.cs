using System;
using System.Collections;
using UnityEngine;
using xasset;
using Logger = xasset.Logger;
using Object = UnityEngine.Object;

namespace FishFramework
{
    [RequireComponent(typeof(Updater))]
    [DisallowMultipleComponent]
    public class ResourceModule : MonoBehaviour
    {
        [Tooltip("是否开启日志")] public bool loggable;

        [Tooltip("文件校验模式，对于带hash的文件名，其实使用 size 校验就可以了")] [SerializeField]
        private VerifyMode verifyMode = VerifyMode.Size;

        public IEnumerator InitializeAsync()
        {
            Versions.VerifyMode = verifyMode;

            //初始化xasset 主要用来完成平台路径的设置以及母包的清单文件的加载
            var operation = Versions.InitializeAsync();
            yield return operation;
            Logger.I("Initialize: {0}", operation.status);
            Logger.I("API Version: {0}", Versions.APIVersion);
            Logger.I("Manifests Version: {0}", Versions.ManifestsVersion);
            Logger.I("SimulationMode: {0}", Versions.SimulationMode);
            Logger.I("OfflineMode: {0}", Versions.OfflineMode);
            Logger.I("EncryptionEnabled: {0}", Versions.EncryptionEnabled);
            Logger.I("Application.platform: {0}", Application.platform);
            Logger.Loggable = loggable;
        }

        public static void LoadAssetAsync(string assetPath, Type type, Action<Asset> func)
        {
            Asset.LoadAsync(assetPath, type, func);
        }

        public static Asset LoadAsset(string assetPath, Type type)
        {
            var asset = Asset.Load(assetPath, type);
            return asset;
        }

        public static void LoadAssetAsync<T>(string assetPath, Action<T> func) where T : Object
        {
            Asset.LoadAsync(assetPath, typeof(T), asset =>
            {
                if (asset == null)
                {
                    func?.Invoke(null);
                }
                else
                {
                    func?.Invoke(asset.asset as T);
                }
            });
        }

        public static T LoadAsset<T>(string assetPath) where T : Object
        {
            Asset asset = Asset.Load(assetPath, typeof(T));

            return asset?.asset as T;
        }

        /// <summary>
        /// 释放当前可释放的内存（资源缓存、未使用资源、GC）。
        /// </summary>
        /// <returns>Unity 异步释放操作，调用方可选择等待完成。</returns>
        public static AsyncOperation ReleaseAllReleasableMemory()
        {
            // 先清理 xasset 持有的缓存和 AssetBundle，再触发 Unity 未使用资源卸载。
            // Loadable.ClearAll();
            GC.Collect();
            var unloadOp = Resources.UnloadUnusedAssets();
            GC.Collect();
            return unloadOp;
        }
    }
}