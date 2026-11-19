using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using xasset;
using FishFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FishXSortPlay
{
    /// <summary>
    /// 关卡布局按块加载：打包与运行时共用 ChunkSize。
    /// 进关只加载当前这一关。离开所在块（每 ChunkSize 关）时释放已加载的关卡。
    /// </summary>
    public static class LevelChunkLoader
    {
        // ponytail: 块大小与打包 customPacker 必须同一常量；要改粒度只改这里。
        public const int ChunkSize = 50;

        /// <summary>打包反查一次覆盖的名单长度。进关只解析当前这一关，不在点击时扫这一页。</summary>
        public const int PlayOrderPageSize = 1000;

        /// <summary>无尽池下限（1-based），打包 max 至少要盖住它。</summary>
        public const int InfinitePoolMinLevel = 34;

        /// <summary>无尽池上限（1-based）。</summary>
        public const int InfinitePoolMaxLevel = 84;

        static int s_loadedChunkId = -1;
        static readonly Dictionary<int, LevelModel> s_levels = new();
        static readonly Dictionary<int, Asset> s_assets = new();
        static string[] s_playOrder;
        static string s_playOrderKey;
        static Dictionary<string, int> s_playIndexByFile;
        static int s_chunkGeneration;
        static readonly Dictionary<int, List<Action>> s_levelWaiters = new();

        public static int GetChunkId(int zeroBasedIndex)
        {
            if (zeroBasedIndex < 0)
            {
                return 0;
            }

            return zeroBasedIndex / ChunkSize;
        }

        public static bool ShouldPackLevel(int oneBasedLevelNum, int maxLevelNum)
        {
            return oneBasedLevelNum >= 1 && oneBasedLevelNum <= maxLevelNum;
        }

        public static int GetConfiguredMaxLevelNum()
        {
            GameSettings settings = null;
            try
            {
                if (GameModule.Setting != null)
                {
                    settings = GameModule.Setting.GetGameSettings();
                }
            }
            catch
            {
                settings = null;
            }

            if (settings == null)
            {
                settings = Resources.Load<GameSettings>(nameof(GameSettings));
            }

            if (settings == null)
            {
                return 1000;
            }

            return LevelPlatformResolver.GetCurrentPlatform() == LevelPlatform.IOS
                ? settings.iosMaxLevelNum
                : settings.androidMaxLevelNum;
        }

        /// <summary>
        /// 进关路径跟左侧列表 # 走，不跟文件名里的数字走。
        /// 编辑器按页读 Level Database（不碰 LevelModel）；打包后读 LevelPlayOrder_*.txt。
        /// </summary>
        public static void ClearPlayOrderCache()
        {
            s_playOrder = null;
            s_playOrderKey = null;
            s_playIndexByFile = null;
        }

        public static bool HasPlayOrder()
        {
            return EnsurePlayOrderShell();
        }

        public static string GetLevelPath(int zeroBasedIndex)
        {
            EnsurePlayOrderPage(zeroBasedIndex);
            if (s_playOrder != null && s_playOrder.Length > 0)
            {
                if (zeroBasedIndex < 0 || zeroBasedIndex >= s_playOrder.Length
                    || string.IsNullOrEmpty(s_playOrder[zeroBasedIndex]))
                {
                    Debug.LogError($"Level play order has no entry for index {zeroBasedIndex}.");
                    return $"{LevelPlatformResolver.GetLevelsFolder()}/__missing_play_order_{zeroBasedIndex}.asset";
                }

                return $"{LevelPlatformResolver.GetLevelsFolder()}/{s_playOrder[zeroBasedIndex]}.asset";
            }

            Debug.LogError(
                $"Level play order is missing for index {zeroBasedIndex}. " +
                "Load order follows the Level Database list, not the number in the file name.");
            return $"{LevelPlatformResolver.GetLevelsFolder()}/__missing_play_order_{zeroBasedIndex}.asset";
        }

        public static bool TryGetPlayIndex(string assetPath, out int zeroBasedIndex)
        {
            zeroBasedIndex = -1;
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            EnsurePlayIndexMap();
            if (s_playIndexByFile == null)
            {
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath.Replace('\\', '/'));
            return s_playIndexByFile.TryGetValue(fileName, out zeroBasedIndex);
        }

        static string CurrentPlayOrderKey()
        {
            return LevelPlatformResolver.GetCurrentPlatform() == LevelPlatform.IOS
                ? "LevelPlayOrder_IOS"
                : "LevelPlayOrder_Android";
        }

        /// <summary>
        /// 只建名单长度，不解析条目、不加载关卡资源。
        /// 编辑器里条目按 PlayOrderPageSize 分页，在 <see cref="EnsurePlayOrderPage"/> 里补。
        /// </summary>
        static bool EnsurePlayOrderShell()
        {
            string key = CurrentPlayOrderKey();
            if (s_playOrder != null && s_playOrderKey == key)
            {
                return s_playOrder.Length > 0;
            }

            s_playOrderKey = key;
            s_playIndexByFile = null;
            s_playOrder = ReadPlayOrder(key);
            if (s_playOrder != null && s_playOrder.Length > 0)
            {
                return true;
            }

#if UNITY_EDITOR
            LevelPlatform platform = key == "LevelPlayOrder_IOS" ? LevelPlatform.IOS : LevelPlatform.Android;
            if (TryCreateEditorPlayOrderShell(platform, out string[] shell))
            {
                s_playOrder = shell;
                return true;
            }
#endif
            return false;
        }

        static void EnsurePlayOrderPage(int zeroBasedIndex)
        {
            if (!EnsurePlayOrderShell() || s_playOrder == null)
            {
                return;
            }

            if (zeroBasedIndex < 0 || zeroBasedIndex >= s_playOrder.Length)
            {
                return;
            }

            // null 才是还没解析。空字符串表示这一格没有关卡，不要再去扫一整页。
            if (s_playOrder[zeroBasedIndex] != null)
            {
                return;
            }

#if UNITY_EDITOR
            FillEditorPlayOrderRange(zeroBasedIndex, zeroBasedIndex + 1);
#else
            s_playOrder[zeroBasedIndex] = "";
#endif
        }

#if UNITY_EDITOR
        static bool TryCreateEditorPlayOrderShell(LevelPlatform platform, out string[] shell)
        {
            shell = null;
            SerializedProperty levels = GetEditorLevels(platform, out SerializedObject serialized);
            if (levels == null || serialized == null)
            {
                return false;
            }

            // 长度来自 arraySize。不要读 Levels[i]，那会把每一关资源都加载进来。
            shell = new string[levels.arraySize];
            return serialized.targetObject != null && shell.Length > 0;
        }

        static void FillEditorPlayOrderRange(int start, int end)
        {
            if (s_playOrder == null)
            {
                return;
            }

            LevelPlatform platform = s_playOrderKey == "LevelPlayOrder_IOS"
                ? LevelPlatform.IOS
                : LevelPlatform.Android;
            start = Mathf.Clamp(start, 0, s_playOrder.Length);
            end = Mathf.Clamp(end, start, s_playOrder.Length);
            SerializedProperty levels = GetEditorLevels(platform, out SerializedObject serialized);
            for (int i = start; i < end; i++)
            {
                s_playOrder[i] = levels != null && serialized != null
                    ? ReadEditorLevelName(levels, i)
                    : "";
                if (s_playIndexByFile != null
                    && !string.IsNullOrEmpty(s_playOrder[i])
                    && !s_playIndexByFile.ContainsKey(s_playOrder[i]))
                {
                    s_playIndexByFile.Add(s_playOrder[i], i);
                }
            }
        }

        static SerializedProperty GetEditorLevels(LevelPlatform platform, out SerializedObject serialized)
        {
            serialized = null;
            LevelCatalog database = LevelPlatformResolver.LoadDatabaseInEditor(platform);
            if (database == null)
            {
                return null;
            }

            serialized = new SerializedObject(database);
            SerializedProperty levels = serialized.FindProperty("levels");
            if (levels == null || !levels.isArray || levels.arraySize == 0)
            {
                return null;
            }

            return levels;
        }

        static string ReadEditorLevelName(SerializedProperty levels, int index)
        {
            if (levels == null || index < 0 || index >= levels.arraySize)
            {
                return "";
            }

            int instanceId = levels.GetArrayElementAtIndex(index).objectReferenceInstanceIDValue;
            if (instanceId == 0
                || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceId, out string guid, out long _))
            {
                return "";
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(assetPath)
                ? ""
                : Path.GetFileNameWithoutExtension(assetPath);
        }
#endif

        static string[] ReadPlayOrder(string resourceName)
        {
#if UNITY_EDITOR
            string diskPath = Path.Combine(Application.dataPath, "Resources", resourceName + ".txt");
            if (File.Exists(diskPath))
            {
                return TrimOrderLines(File.ReadAllLines(diskPath));
            }
#endif
            TextAsset text = Resources.Load<TextAsset>(resourceName);
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                return null;
            }

            return TrimOrderLines(text.text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None));
        }

        static string[] TrimOrderLines(string[] lines)
        {
            int end = lines.Length;
            if (end > 0 && string.IsNullOrWhiteSpace(lines[end - 1]))
            {
                end--;
            }

            var trimmed = new string[end];
            for (int i = 0; i < end; i++)
            {
                trimmed[i] = lines[i].Trim();
            }

            return trimmed;
        }

        static void EnsurePlayIndexMap()
        {
            if (s_playIndexByFile != null)
            {
                return;
            }

            if (!EnsurePlayOrderShell() || s_playOrder == null)
            {
                return;
            }

            // 打包反查只做一次，不走点击进关。进关本身只解析当前关。
            int need = Mathf.Min(s_playOrder.Length, Mathf.Max(1, GetConfiguredMaxLevelNum()));
#if UNITY_EDITOR
            if (need > 0 && s_playOrder[0] == null)
            {
                FillEditorPlayOrderRange(0, need);
            }
#endif

            s_playIndexByFile = new Dictionary<string, int>(need);
            for (int i = 0; i < s_playOrder.Length; i++)
            {
                if (!string.IsNullOrEmpty(s_playOrder[i]) && !s_playIndexByFile.ContainsKey(s_playOrder[i]))
                {
                    s_playIndexByFile.Add(s_playOrder[i], i);
                }
            }
        }

        public static LevelModel Get(int zeroBasedIndex)
        {
            return s_levels.TryGetValue(zeroBasedIndex, out LevelModel level) ? level : null;
        }

        public static int FindIndex(LevelModel level)
        {
            if (level == null)
            {
                return -1;
            }

            foreach (KeyValuePair<int, LevelModel> pair in s_levels)
            {
                if (pair.Value == level)
                {
                    return pair.Key;
                }
            }

            return -1;
        }

        public static bool IsChunkLoaded(int zeroBasedIndex)
        {
            return HasLiveLevel(zeroBasedIndex);
        }

        static bool HasLiveLevel(int zeroBasedIndex)
        {
            if (!s_levels.TryGetValue(zeroBasedIndex, out LevelModel level))
            {
                return false;
            }

            if (level != null)
            {
                return true;
            }

            // 退出 Play 会卸掉资源，字典里会留下已销毁对象。下次点击要重新加载这一关。
            s_levels.Remove(zeroBasedIndex);
            if (s_assets.TryGetValue(zeroBasedIndex, out Asset asset))
            {
                asset?.ReleaseRef();
                s_assets.Remove(zeroBasedIndex);
            }

            return false;
        }

        /// <summary>同步确保这一关已加载。同一块里的其他关不预先加载。</summary>
        public static void EnsureChunkSync(int zeroBasedIndex)
        {
            KeepChunk(zeroBasedIndex);
            if (HasLiveLevel(zeroBasedIndex))
            {
                return;
            }

            LevelModel level = LoadLevelImmediate(GetLevelPath(zeroBasedIndex), out Asset asset);
            StoreLevel(zeroBasedIndex, level, asset);
        }

        /// <summary>异步确保这一关已加载。点关卡 34 只等第 34 关，不等同块另外 49 关。</summary>
        public static void EnsureChunk(int zeroBasedIndex, Action onReady)
        {
            KeepChunk(zeroBasedIndex);
            if (HasLiveLevel(zeroBasedIndex))
            {
                onReady?.Invoke();
                return;
            }

            if (!s_levelWaiters.TryGetValue(zeroBasedIndex, out List<Action> waiters))
            {
                waiters = new List<Action>();
                s_levelWaiters[zeroBasedIndex] = waiters;
                if (onReady != null)
                {
                    waiters.Add(onReady);
                }

                int generation = s_chunkGeneration;
                int index = zeroBasedIndex;
                LoadLevelAsync(GetLevelPath(index), index, generation, () =>
                {
                    s_levelWaiters.Remove(index);
                    if (generation != s_chunkGeneration)
                    {
                        return;
                    }

                    for (int i = 0; i < waiters.Count; i++)
                    {
                        waiters[i]?.Invoke();
                    }
                });
                return;
            }

            if (onReady != null)
            {
                waiters.Add(onReady);
            }
        }

        static void KeepChunk(int zeroBasedIndex)
        {
            int chunkId = GetChunkId(zeroBasedIndex);
            if (s_loadedChunkId == chunkId)
            {
                return;
            }

            UnloadLoadedChunk();
            s_loadedChunkId = chunkId;
        }

        static void StoreLevel(int zeroBasedIndex, LevelModel level, Asset asset)
        {
            if (level == null)
            {
                return;
            }

            s_levels[zeroBasedIndex] = level;
            if (asset != null)
            {
                s_assets[zeroBasedIndex] = asset;
            }
        }

        public static void UnloadAll()
        {
            UnloadLoadedChunk();
        }

        static void UnloadLoadedChunk()
        {
            s_chunkGeneration++;
            s_levelWaiters.Clear();
            s_levels.Clear();
            foreach (KeyValuePair<int, Asset> pair in s_assets)
            {
                pair.Value?.ReleaseRef();
            }

            s_assets.Clear();
            s_loadedChunkId = -1;
        }

        static LevelModel LoadLevelImmediate(string path, out Asset asset)
        {
            asset = null;
            try
            {
                asset = ResourceModule.LoadAsset(path, typeof(LevelModel));
                if (asset != null && asset.asset is LevelModel fromXasset)
                {
                    return fromXasset;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelChunkLoader] xasset load failed: {path}\n{e.Message}");
            }

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<LevelModel>(path);
#else
            return null;
#endif
        }

        static void LoadLevelAsync(string path, int zeroBased, int generation, Action onDone)
        {
            bool finished = false;

            void Finish(LevelModel level, Asset asset)
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                if (generation != s_chunkGeneration)
                {
                    asset?.ReleaseRef();
                    return;
                }

                StoreLevel(zeroBased, level, asset);
                onDone?.Invoke();
            }

            Asset request = null;
            try
            {
                request = Asset.LoadAsync(path, typeof(LevelModel), loaded =>
                {
                    if (loaded != null && loaded.asset is LevelModel level)
                    {
                        Finish(level, loaded);
                        return;
                    }

#if UNITY_EDITOR
                    Finish(AssetDatabase.LoadAssetAtPath<LevelModel>(path), null);
#else
                    Finish(null, null);
#endif
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelChunkLoader] async load failed: {path}\n{e.Message}");
                request = null;
            }

            if (request != null)
            {
                return;
            }

#if UNITY_EDITOR
            Finish(AssetDatabase.LoadAssetAtPath<LevelModel>(path), null);
#else
            Finish(null, null);
#endif
        }
    }
}
