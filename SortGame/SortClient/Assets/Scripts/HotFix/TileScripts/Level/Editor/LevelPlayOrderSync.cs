using System.IO;
using System.Text;
using FishFramework;
using UnityEditor;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 左侧列表改顺序后，把 Level Database 的 levels 数组写回 LevelPlayOrder_*.txt。
    /// 进关读这份名单，不读文件名里的数字。
    /// </summary>
    public class LevelPlayOrderSync : AssetModificationProcessor
    {
        static bool s_writing;

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (s_writing || paths == null)
            {
                return paths;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i].Replace('\\', '/');
                if (path == LevelPlatformResolver.AndroidDatabasePath)
                {
                    TryRewrite(LevelPlatform.Android);
                }
                else if (path == LevelPlatformResolver.IOSDatabasePath)
                {
                    TryRewrite(LevelPlatform.IOS);
                }
            }

            return paths;
        }

        [InitializeOnLoadMethod]
        static void HookPlayMode()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.ExitingEditMode)
                {
                    return;
                }

                LevelChunkLoader.ClearPlayOrderCache();
            };
        }

        /// <summary>
        /// Cheap path after a single list drag: permute lines in the existing play-order file.
        /// No per-slot AssetDatabase lookups. Returns false if the file is missing/mismatched.
        /// </summary>
        public static bool TryRewriteByMovingSlot(LevelPlatform platform, int from, int to)
        {
            if (s_writing || from < 0 || to < 0 || from == to)
            {
                return false;
            }

            string resourceName = platform == LevelPlatform.IOS ? "LevelPlayOrder_IOS" : "LevelPlayOrder_Android";
            string diskPath = Path.Combine(Application.dataPath, "Resources", resourceName + ".txt");
            if (!File.Exists(diskPath))
            {
                return false;
            }

            string[] lines = File.ReadAllLines(diskPath);
            int end = lines.Length;
            if (end > 0 && string.IsNullOrWhiteSpace(lines[end - 1]))
            {
                end--;
            }

            if (from >= end || to >= end)
            {
                return false;
            }

            string moved = lines[from].Trim();
            if (from < to)
            {
                for (int i = from; i < to; i++)
                {
                    lines[i] = lines[i + 1];
                }
            }
            else
            {
                for (int i = from; i > to; i--)
                {
                    lines[i] = lines[i - 1];
                }
            }

            lines[to] = moved;

            var builder = new StringBuilder();
            for (int i = 0; i < end; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(lines[i].Trim());
            }

            s_writing = true;
            try
            {
                File.WriteAllText(diskPath, builder.ToString());
                LevelChunkLoader.ClearPlayOrderCache();
            }
            finally
            {
                s_writing = false;
            }

            return true;
        }

        public static void TryRewrite(LevelPlatform platform)
        {
            if (s_writing)
            {
                return;
            }

            LevelCatalog database = AssetDatabase.LoadAssetAtPath<LevelCatalog>(
                LevelPlatformResolver.GetDatabasePath(platform));
            if (database == null)
            {
                return;
            }

            // 用引用的 instanceId 取路径。不要读 Levels[i]，那会把每一关都加载进来。
            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty levels = serializedDatabase.FindProperty("levels");
            if (levels == null || !levels.isArray || levels.arraySize == 0)
            {
                return;
            }

            string resourceName = platform == LevelPlatform.IOS ? "LevelPlayOrder_IOS" : "LevelPlayOrder_Android";
            string diskPath = Path.Combine(Application.dataPath, "Resources", resourceName + ".txt");

            var builder = new StringBuilder();
            for (int i = 0; i < levels.arraySize; i++)
            {
                int instanceId = levels.GetArrayElementAtIndex(i).objectReferenceInstanceIDValue;
                if (instanceId == 0)
                {
                    Debug.LogError($"[LevelPlayOrder] {platform} slot {i + 1} is empty. Play order file was not overwritten.");
                    return;
                }

                string guid;
                long localId;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceId, out guid, out localId))
                {
                    Debug.LogError($"[LevelPlayOrder] {platform} slot {i + 1} has no guid. Play order file was not overwritten.");
                    return;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError($"[LevelPlayOrder] {platform} slot {i + 1} asset is missing. Play order file was not overwritten.");
                    return;
                }

                if (i > 0)
                {
                    builder.Append('\n');
                }

                // Always write the asset currently referenced by levels[i].
                builder.Append(Path.GetFileNameWithoutExtension(assetPath));
            }

            string built = builder.ToString();
            if (File.Exists(diskPath))
            {
                string current = File.ReadAllText(diskPath).TrimEnd('\r', '\n');
                if (string.Equals(current, built, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            s_writing = true;
            try
            {
                File.WriteAllText(diskPath, builder.ToString());
                LevelChunkLoader.ClearPlayOrderCache();
            }
            finally
            {
                s_writing = false;
            }
        }
    }
}
