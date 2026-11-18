#if UNITY_EDITOR
using System.IO;
using FishFramework;
using UnityEditor;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 编辑器里第 N 关以进关名单为准。名单上的文件还在时，不用 Level Database 里可能漂移的 GUID。
    /// </summary>
    public static class EditorListedLevel
    {
        public static LevelPlatform PlatformOf(LevelCatalog database)
        {
            string path = database != null ? AssetDatabase.GetAssetPath(database) : null;
            if (path == LevelPlatformResolver.IOSDatabasePath)
            {
                return LevelPlatform.IOS;
            }

            return LevelPlatform.Android;
        }

        public static string[] ReadOrder(LevelPlatform platform)
        {
            string resourceName = platform == LevelPlatform.IOS ? "LevelPlayOrder_IOS" : "LevelPlayOrder_Android";
            string diskPath = Path.Combine(Application.dataPath, "Resources", resourceName + ".txt");
            if (!File.Exists(diskPath))
            {
                return System.Array.Empty<string>();
            }

            string[] lines = File.ReadAllLines(diskPath);
            int end = lines.Length;
            if (end > 0 && string.IsNullOrWhiteSpace(lines[end - 1]))
            {
                end--;
            }

            string[] trimmed = new string[end];
            for (int i = 0; i < end; i++)
            {
                trimmed[i] = lines[i].Trim();
            }

            return trimmed;
        }

        public static LevelModel Load(LevelCatalog database, string[] order, int index)
        {
            LevelPlatform platform = PlatformOf(database);
            if (order != null && index >= 0 && index < order.Length && !string.IsNullOrEmpty(order[index]))
            {
                string assetPath = LevelPlatformResolver.GetLevelsFolder(platform) + "/" + order[index] + ".asset";
                LevelModel listed = AssetDatabase.LoadAssetAtPath<LevelModel>(assetPath);
                if (listed != null)
                {
                    return listed;
                }
            }

            if (database == null)
            {
                return null;
            }

            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty levels = serialized.FindProperty("levels");
            if (levels == null || !levels.isArray || index < 0 || index >= levels.arraySize)
            {
                return null;
            }

            return levels.GetArrayElementAtIndex(index).objectReferenceValue as LevelModel;
        }
    }
}
#endif
