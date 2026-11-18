#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FishTilePlay;

namespace TileGameCore
{
    /// <summary>
    /// One-shot: migrate legacy levelN assets into platform folders/databases.
    /// Safe to re-run: skips if Level Database Android already exists with levels.
    /// </summary>
    public static class LevelPlatformMigrationTool
    {
        private const string LegacyLevelsFolder = "Assets/Project Files/Data/Level System/Levels";
        private const string LegacyDatabasePath = LevelPlatformResolver.LevelSystemFolder + "/Level Database.asset";

        [MenuItem("Tools/Level Editor/Migrate To Platform Databases", false, 50)]
        public static void Migrate()
        {
            try
            {
                EnsureFolders();

                if (File.Exists(LevelPlatformResolver.AndroidDatabasePath.Replace('/', Path.DirectorySeparatorChar)
                    .Replace("Assets", Application.dataPath)))
                {
                    // Prefer AssetDatabase path check
                }

                var existingAndroid = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelPlatformResolver.AndroidDatabasePath);
                if (existingAndroid != null && existingAndroid.AmountOfLevels > 0)
                {
                    EnsureEmptyIosDatabase();
                    EditorUtility.DisplayDialog("Migrate",
                        "Level Database Android already has levels. Ensured empty iOS DB exists.",
                        "OK");
                    return;
                }

                // 1) Move/rename each legacy levelN.asset → LevelsAndroid/Android_Level_N.asset
                string[] guids = AssetDatabase.FindAssets("t:LevelModel", new[] { LegacyLevelsFolder });
                var numbered = new SortedDictionary<int, string>();
                var numberRegex = new Regex(@"^level(\d+)$", RegexOptions.IgnoreCase);

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    string name = Path.GetFileNameWithoutExtension(path);
                    Match m = numberRegex.Match(name);
                    if (!m.Success)
                    {
                        Debug.LogWarning("[Migrate] Skip non-standard level asset: " + path);
                        continue;
                    }

                    int num = int.Parse(m.Groups[1].Value);
                    numbered[num] = path;
                }

                int moved = 0;
                int total = numbered.Count;
                foreach (var pair in numbered)
                {
                    EditorUtility.DisplayProgressBar("Migrate levels",
                        $"Android_Level_{pair.Key}", (float)moved / Mathf.Max(1, total));

                    int fileId = LevelAssetNaming.ToFileId(pair.Key);
                    string dest = $"{LevelPlatformResolver.AndroidLevelsFolder}/Android_Level_{fileId}.asset";
                    if (AssetDatabase.LoadAssetAtPath<LevelModel>(dest) != null)
                    {
                        moved++;
                        continue;
                    }

                    string error = AssetDatabase.MoveAsset(pair.Value, dest);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[Migrate] Move failed {pair.Value} → {dest}: {error}");
                    }
                    else
                    {
                        moved++;
                    }
                }

                EditorUtility.ClearProgressBar();

                // 2) Promote legacy DB → Android DB (preserve GUID so GameScene keeps working)
                LevelCatalog legacyDb = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LegacyDatabasePath);
                if (legacyDb != null && existingAndroid == null)
                {
                    string renameError = AssetDatabase.MoveAsset(LegacyDatabasePath, LevelPlatformResolver.AndroidDatabasePath);
                    if (!string.IsNullOrEmpty(renameError))
                    {
                        Debug.LogError("[Migrate] Rename Level Database → Android failed: " + renameError);
                    }
                    else
                    {
                        var androidDb = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelPlatformResolver.AndroidDatabasePath);
                        if (androidDb != null)
                        {
                            androidDb.name = "Level Database Android";
                            EditorUtility.SetDirty(androidDb);
                        }
                    }
                }
                else if (existingAndroid == null)
                {
                    // Create Android DB and fill from moved assets in numeric order
                    var androidDb = ScriptableObject.CreateInstance<LevelCatalog>();
                    AssetDatabase.CreateAsset(androidDb, LevelPlatformResolver.AndroidDatabasePath);
                    FillAndroidDatabaseFromFolder(androidDb);
                }
                else
                {
                    FillAndroidDatabaseFromFolder(existingAndroid);
                }

                EnsureEmptyIosDatabase();

                try
                {
                    BuildUtility.ApplyGameSceneLevelDatabaseForBuild();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[Migrate] GameScene sync skipped/failed: " + ex.Message);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Migrate",
                    $"Done. Moved/confirmed {moved} Android levels. iOS DB is empty.",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void EnsureFolders()
        {
            LevelEditorBase.CreateFolderIfNotExist(LevelPlatformResolver.LevelSystemFolder);
            LevelEditorBase.CreateFolderIfNotExist(LevelPlatformResolver.AndroidLevelsFolder);
            LevelEditorBase.CreateFolderIfNotExist(LevelPlatformResolver.IOSLevelsFolder);
            AssetDatabase.Refresh();
        }

        private static void EnsureEmptyIosDatabase()
        {
            var iosDb = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelPlatformResolver.IOSDatabasePath);
            if (iosDb != null)
            {
                return;
            }

            // Clone Android DB shell (tiles/effects/colors) with empty levels when possible
            var androidDb = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelPlatformResolver.AndroidDatabasePath);
            LevelCatalog created;
            if (androidDb != null)
            {
                created = Object.Instantiate(androidDb);
                var so = new SerializedObject(created);
                var levelsProp = so.FindProperty("levels");
                if (levelsProp != null && levelsProp.isArray)
                {
                    levelsProp.arraySize = 0;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                created = ScriptableObject.CreateInstance<LevelCatalog>();
            }

            created.name = "Level Database IOS";
            AssetDatabase.CreateAsset(created, LevelPlatformResolver.IOSDatabasePath);
            EditorUtility.SetDirty(created);
        }

        private static void FillAndroidDatabaseFromFolder(LevelCatalog androidDb)
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelModel", new[] { LevelPlatformResolver.AndroidLevelsFolder });
            var ordered = new SortedDictionary<int, LevelModel>();
            var numberRegex = new Regex(@"Android_Level_(\d+)$", RegexOptions.IgnoreCase);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Match m = numberRegex.Match(Path.GetFileNameWithoutExtension(path));
                if (!m.Success)
                {
                    continue;
                }

                int num = int.Parse(m.Groups[1].Value);
                ordered[num] = AssetDatabase.LoadAssetAtPath<LevelModel>(path);
            }

            var so = new SerializedObject(androidDb);
            var levelsProp = so.FindProperty("levels");
            if (levelsProp == null)
            {
                return;
            }

            levelsProp.arraySize = ordered.Count;
            int index = 0;
            foreach (var pair in ordered)
            {
                levelsProp.GetArrayElementAtIndex(index).objectReferenceValue = pair.Value;
                index++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(androidDb);
        }
    }
}
#endif
