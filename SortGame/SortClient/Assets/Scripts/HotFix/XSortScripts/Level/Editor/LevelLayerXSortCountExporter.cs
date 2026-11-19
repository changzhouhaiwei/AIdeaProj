#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace FishXSortPlay
{
    public static class LevelLayerXSortCountExporter
    {
        private const string ExportFileName = "LevelLayerXSortCount.xlsx";
        private const string EggSmashConfigPath = "Assets/CABRes/Configs/game_tbcsvcoloregglevel.json";

        private static string ExportDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem("Tools/Level Editor/导出关卡每层棋子数", false, 1)]
        [MenuItem("Tools/关卡验证器/导出关卡每层棋子数")]
        private static void ExportFromMenu()
        {
            LevelCatalog database = LevelPlatformResolver.LoadDatabaseInEditor();
            if (database == null)
            {
                Debug.LogError("[LevelExport] LevelCatalog not found for current platform.");
                return;
            }

            ExportRange(database.Levels, 0, database.AmountOfLevels - 1);
        }

        public static string ExportAll(LevelModel[] levels)
        {
            int end = levels == null ? -1 : levels.Length - 1;
            return ExportRange(levels, 0, end);
        }

        /// <summary>
        /// 关卡号 = 左侧列表序号（从 1 开始）。数据 = 该序号在右侧实际打开的关卡。
        /// </summary>
        public static string ExportRange(LevelModel[] referencedLevels, int startIndex, int endIndex)
        {
            if (referencedLevels == null || referencedLevels.Length == 0 || endIndex < startIndex)
            {
                Debug.LogWarning("[LevelExport] No levels to export.");
                return null;
            }

            startIndex = Mathf.Clamp(startIndex, 0, referencedLevels.Length - 1);
            endIndex = Mathf.Clamp(endIndex, startIndex, referencedLevels.Length - 1);

            List<IList<string>> rows = new List<IList<string>>
            {
                new List<string>
                {
                    "关卡号",
                    "第1层棋子数",
                    "第2层棋子数",
                    "第3层棋子数",
                    "总层数",
                    "set数",
                    "总棋子数",
                    "boxcover",
                    "eggsmash",
                    "unknown"
                }
            };

            HashSet<int> eggSmashLevels = LoadEggSmashLevels();

            for (int index = startIndex; index <= endIndex; index++)
            {
                LevelModel level = LevelListSlot.Resolve(referencedLevels[index], index);
                if (level == null)
                {
                    continue;
                }

                int levelNumber = index + 1;
                rows.Add(new List<string>
                {
                    levelNumber.ToString(),
                    GetLayerXSortCount(level, 0).ToString(),
                    GetLayerXSortCount(level, 1).ToString(),
                    GetLayerXSortCount(level, 2).ToString(),
                    level.AmountOfLayers.ToString(),
                    level.SetsAmount.ToString(),
                    level.GetAmountOfFilledCells().ToString(),
                    Flag(level.HasBoxCovers),
                    Flag(eggSmashLevels.Contains(levelNumber)),
                    Flag(level.ContainsEffect(XSortEffectType.Unknown))
                });
            }

            string exportPath = Path.Combine(ExportDirectory, ExportFileName);
            SimpleXlsxWriter.Write(exportPath, rows);
            Debug.Log($"[LevelExport] Exported {rows.Count - 1} levels by editor list # to {exportPath}");
            EditorUtility.RevealInFinder(exportPath);
            return exportPath;
        }

        private static string Flag(bool present)
        {
            return present ? "1" : "0";
        }

        /// <summary>
        /// eggsmash 关号来自彩蛋关表，与局内 AwardColorEggCoverConfig 用的是同一份配置。
        /// </summary>
        public static HashSet<int> LoadEggSmashLevels()
        {
            HashSet<int> levels = new HashSet<int>();
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(EggSmashConfigPath);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                Debug.LogWarning("[LevelExport] eggsmash config missing: " + EggSmashConfigPath);
                return levels;
            }

            JSONNode root = JSON.Parse(asset.text);
            if (root == null)
            {
                Debug.LogWarning("[LevelExport] eggsmash config parse failed: " + EggSmashConfigPath);
                return levels;
            }

            foreach (JSONNode row in root.Children)
            {
                int level = row["level"].AsInt;
                if (level > 0)
                {
                    levels.Add(level);
                }
            }

            return levels;
        }

        private static int GetLayerXSortCount(LevelModel level, int layerIndex)
        {
            if (level == null || layerIndex < 0 || layerIndex >= level.AmountOfLayers)
            {
                return 0;
            }

            Layer layer = level.GetLayer(layerIndex);
            return layer != null ? layer.GetAmountOfFilledCells() : 0;
        }
    }
}
#endif
