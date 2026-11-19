#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// When a LevelModel asset under LevelsAndroid / LevelsIOS is renamed in the Project window,
    /// keep LevelModel.dataId equal to the asset file name and refresh the open Level Editor list.
    /// </summary>
    public class LevelAssetNameSyncPostprocessor : AssetPostprocessor
    {
        private static int suppressCount;

        public static void BeginSuppress()
        {
            suppressCount++;
        }

        public static void EndSuppress()
        {
            if (suppressCount > 0)
            {
                suppressCount--;
            }
        }

        private static bool IsSuppressed => suppressCount > 0;

        private static bool IsPlatformLevelPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return assetPath.StartsWith(LevelPlatformResolver.AndroidLevelsFolder + "/", System.StringComparison.Ordinal)
                || assetPath.StartsWith(LevelPlatformResolver.IOSLevelsFolder + "/", System.StringComparison.Ordinal);
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (IsSuppressed)
            {
                return;
            }

            HashSet<string> candidatePaths = new HashSet<string>();

            if (importedAssets != null)
            {
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    if (IsPlatformLevelPath(importedAssets[i]))
                    {
                        candidatePaths.Add(importedAssets[i]);
                    }
                }
            }

            if (movedAssets != null)
            {
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    if (IsPlatformLevelPath(movedAssets[i]))
                    {
                        candidatePaths.Add(movedAssets[i]);
                    }
                }
            }

            if (candidatePaths.Count == 0)
            {
                return;
            }

            bool anySynced = false;
            foreach (string path in candidatePaths)
            {
                LevelModel levelData = AssetDatabase.LoadAssetAtPath<LevelModel>(path);
                if (levelData == null)
                {
                    continue;
                }

                if (string.Equals(levelData.DataId, levelData.name, System.StringComparison.Ordinal))
                {
                    continue;
                }

                LevelsHandler.SyncDataIdToAssetName(levelData);
                anySynced = true;
            }

            if (anySynced)
            {
                AssetDatabase.SaveAssets();
                LevelEditorWindow.RefreshOpenLevelListLabels();
            }
        }
    }
}
#endif
