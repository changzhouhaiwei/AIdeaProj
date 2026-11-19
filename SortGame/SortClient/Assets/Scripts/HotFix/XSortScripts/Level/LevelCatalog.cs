#pragma warning disable 0649

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XSortGameCore;
using UnityEngine.Tilemaps;
using cfg.Game;

namespace FishXSortPlay
{
    [CreateAssetMenu(menuName = "Content/Level Database/Level Database", fileName = "Level Database")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] LevelModel[] levels;
        public LevelModel[] Levels => levels;

        [SerializeField] XSortModel[] xSorts;
        public XSortModel[] XSorts => xSorts;

        [SerializeField] XSortEffect[] xSortEffects;
        public XSortEffect[] XSortEffects => xSortEffects;

        [SerializeField] BackgroundModel[] backgroundData;
        public BackgroundModel[] BackgroundModel => backgroundData;

        // Editor stuff
        [SerializeField, LevelEditorSetting] Color selectedColor = new(0.204f, 1f, 0f, 1f);
        [SerializeField, LevelEditorSetting] Color unselectedColor = new(1f, 1f, 1f, 0f);
        [SerializeField, LevelEditorSetting] Color tintColor = new(0f, 0f, 0f, 0.11f);
        [SerializeField, LevelEditorSetting] Color backgroundColor = new(0.576f, 0.576f, 0.576f, 0.823f);
        [SerializeField, LevelEditorSetting] Color gridColor = new(0, 0, 0, 0.537f);

        public int AmountOfLevels
        {
            get
            {
                if (levels != null && levels.Length > 0)
                {
                    return levels.Length;
                }

                return LevelChunkLoader.GetConfiguredMaxLevelNum();
            }
        }

        public void Init()
        {
            if (xSorts != null)
            {
                for (int i = 0; i < xSorts.Length; i++)
                {
                    if (xSorts[i] != null)
                    {
                        xSorts[i].Init(i);
                    }
                }
            }

            if (xSortEffects == null) return;

            for (int i = 0; i < xSortEffects.Length; i++)
            {
                if (xSortEffects[i] == null)
                {
                    Debug.LogError($"LevelCatalog.xSortEffects[{i}] is missing. Assign all XSortEffect prefabs in Level Database.");
                    continue;
                }

                xSortEffects[i].Init();
            }
        }

        public void Unload()
        {
            if (xSorts != null)
            {
                for (int i = 0; i < xSorts.Length; i++)
                {
                    xSorts[i]?.Unload();
                }
            }

            if (xSortEffects != null)
            {
                for (int i = 0; i < xSortEffects.Length; i++)
                {
                    xSortEffects[i]?.Unload();
                }
            }

            LevelChunkLoader.UnloadAll();
        }

        public int GetRandomLevelIndex(int displayLevelNumber, int lastPlayedLevelNumber, bool replayingLevel)
        {
            if (levels != null && levels.Length > 0)
            {
                if (levels.IsInRange(displayLevelNumber))
                {
                    return displayLevelNumber;
                }

                if (replayingLevel)
                {
                    return lastPlayedLevelNumber;
                }

                int randomLevelIndex;

                do
                {
                    randomLevelIndex = Random.Range(0, levels.Length);
                }
                while (!levels[randomLevelIndex].UseInRandomizer && randomLevelIndex != lastPlayedLevelNumber);

                return randomLevelIndex;
            }

            int maxIndex = Mathf.Max(0, AmountOfLevels - 1);
            return Mathf.Clamp(displayLevelNumber, 0, maxIndex);
        }

        public LevelModel FetchLevelData(int i)
        {
            LevelModel fromChunk = LevelChunkLoader.Get(i);
            if (fromChunk != null)
            {
                return fromChunk;
            }

            if (levels != null && i >= 0 && i < levels.Length)
            {
                return levels[i];
            }

            return null;
        }

        public XSortModel GetXSort(int i)
        {
            if (i < xSorts.Length && i >= 0)
                return xSorts[i];

            return null;
        }

        /// <summary>
        /// 从完整棋子池中挑一个本关未使用的 XSortModel，作为金币匹配载体。
        /// </summary>
        public XSortModel PickUnusedXSortData(XSortModel[] usedxSorts, int excludeXSortIndex = -1)
        {
            if (xSorts == null || xSorts.Length == 0)
            {
                return null;
            }

            HashSet<int> usedIndices = new();
            if (usedxSorts != null)
            {
                for (int i = 0; i < usedxSorts.Length; i++)
                {
                    if (usedxSorts[i] != null)
                    {
                        usedIndices.Add(usedxSorts[i].XSortIndex);
                    }
                }
            }

            usedIndices.Add(excludeXSortIndex);

            for (int i = 0; i < xSorts.Length; i++)
            {
                XSortModel candidate = xSorts[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.CollectionID == 100)
                {
                    continue;
                }

                if (!usedIndices.Contains(candidate.XSortIndex))
                {
                    return candidate;
                }
            }

            return null;
        }


        //根据关卡从GameBgShow中拿到XSortIndexInclude,这是当前levels获得
        //XSortData外显注意  [SerializeField] private int index; //但是不可更改

        //数组中的id是index 必然加入返回的数据中,除非数据量超了.
        //然后进入原来有逻辑获得集合,原有逻辑需要判定ileData中的CollectionId为100的不加入判定.

    
        // public XSortModel[] AvailableForLevel(LevelModel level, int dynamicPieceNumber = 0)
        // {
     
        //     int levelIndex = System.Array.FindIndex(levels, x => x == level);
        //     if (levelIndex != -1)
        //     {
        //         return AvailableForLevel(levelIndex, dynamicPieceNumber);
        //     }

        //     return AvailableForLevel(0, dynamicPieceNumber);
        // }

        public XSortModel[] AvailableForLevel(LevelModel level, int dynamicPieceNumber)
        {
            return AvailableForLevel(level, dynamicPieceNumber, false);
        }

        public XSortModel[] AvailableForLevel(LevelModel level, int dynamicPieceNumber, bool isFirstEnter)
        {
            int levelIndex = -1;
            if (levels != null && levels.Length > 0)
            {
                levelIndex = System.Array.FindIndex(levels, x => x == level);
            }

            if (levelIndex < 0)
            {
                levelIndex = LevelChunkLoader.FindIndex(level);
            }

            if (levelIndex < 0)
            {
                levelIndex = Mathf.Max(0, LevelDirector.LoadedLevelIndex);
            }

            return AvailableForLevelFromeCfg(levelIndex, dynamicPieceNumber, isFirstEnter);
        }


        public XSortModel[] AvailableForLevelFromeCfg(int levelIdx, int dynamicPieceNumber = 0)
        {
            return AvailableForLevelFromeCfg(levelIdx, dynamicPieceNumber, false);
        }

        public XSortModel[] AvailableForLevelFromeCfg(int levelIdx, int dynamicPieceNumber, bool isFirstEnter)
        {
            LevelModel levelData = FetchLevelData(levelIdx);
            if (levelData == null)
            {
                Debug.LogError($"[LevelCatalog] AvailableForLevelFromeCfg: missing LevelModel at index {levelIdx}");
                return System.Array.Empty<XSortModel>();
            }
            for (int i = 0; i < xSorts.Length; i++)
            {
                xSorts[i].ClearRuntimeSkin();
            }

            List<XSortModel> result = new();
            // 旧花色资源废弃后，XSortData只作为对象池和匹配组载体，开放限制由xSortsNew配置控制。
            for (int i = 0; i < xSorts.Length; i++)
            {
                if (xSorts[i].CollectionID != 100)
                {
                    result.Add(xSorts[i]);
                }
            }

            if (dynamicPieceNumber == 0)
            {
                dynamicPieceNumber = levelData.ElementsPerLevel;
            }

            result.RandomizeOrder();

            LevelDirector.MarkColorChallenge(levelIdx, false);
            if (LevelDirector.BInCoinRewardLevel)
            {
                return AvailableForCoinLevel(levelIdx, levelData, result);
            }

            //根据关卡的元素限制数量, 从结果列表中随机选择元素
            int elementsPerLevel = Mathf.Clamp(dynamicPieceNumber, 1, result.Count);
            if (result.Count > elementsPerLevel)
            {
                result.RemoveRange(elementsPerLevel, result.Count - elementsPerLevel);
            }

            int normalXSortCount = result.Count;
            string colorChallenge = LevelDynamicConfigProvider.GetLevelConfig(levelIdx + 1)?.Colorchallenge;
            int levelNumber = levelIdx + 1;
            bool colorEnabled = ActivityData.IsColorChallengeEnabled() && !string.IsNullOrEmpty(colorChallenge);
            int colorSkinTotal = colorEnabled
                ? XSortSkinCatalog.CountColorChallengeSkins(colorChallenge, levelNumber)
                : 0;
            bool useColor = colorEnabled && colorSkinTotal >= dynamicPieceNumber;
            LevelDirector.MarkColorChallenge(levelIdx, useColor);
            if (colorEnabled && !useColor)
            {
#if UNITY_EDITOR
                Debug.LogError($"level={levelNumber} color={colorChallenge} 颜色棋子总{colorSkinTotal} 不够配置 {dynamicPieceNumber}");
#endif
            }

            List<XSortSkinRuntimeData> levelSkins;
            if (useColor)
            {
                levelSkins = XSortSkinCatalog.BuildColorChallengeSkins(
                    colorChallenge,
                    levelNumber,
                    normalXSortCount
                );
            }
            else
            {
                levelSkins = XSortSkinCatalog.BuildLevelSkins(
                    dynamicPieceNumber,
                    levelIdx + 1,
                    normalXSortCount,
                    levelData.GetAmountOfFilledCells(),
                    isFirstEnter
                );
            }

            if (levelSkins.Count > 0)
            {
                if (levelSkins.Count < normalXSortCount)
                {
                    result.RemoveRange(levelSkins.Count, normalXSortCount - levelSkins.Count);
                }

                for (int i = 0; i < levelSkins.Count; i++)
                {
                    result[i].SetRuntimeSkin(levelSkins[i]);
                }
            }
            else
            {
                // 新资源配置缺失时保留旧兜底，避免关卡无法生成。
                // Level Editor Test Level also needs this when ConfigHelper skins fail to build.
                CheckXSortListByCfg(ref result, levelIdx, 0);
            }


            return result.ToArray();
        }

        XSortModel[] AvailableForCoinLevel(int levelIdx, LevelModel levelData, List<XSortModel> result)
        {
            LevelDirector.GetCoinRewardSplit(
                levelData.GetAmountOfFilledCells(),
                out int coinXSortCount,
                out int otherGroupCount
            );

            List<XSortSkinRuntimeData> coinSkins = XSortSkinCatalog.BuildCoinLevelSkins(
                otherGroupCount,
                levelIdx + 1,
                coinXSortCount
            );

            int normalXSortCount = result.Count;
            if (coinSkins.Count <= 0)
            {
                if (result.Count > 0)
                {
                    result.RemoveRange(0, result.Count);
                }

                return result.ToArray();
            }

            if (coinSkins.Count < normalXSortCount)
            {
                result.RemoveRange(coinSkins.Count, normalXSortCount - coinSkins.Count);
            }
            else if (coinSkins.Count > normalXSortCount)
            {
                coinSkins.RemoveRange(normalXSortCount, coinSkins.Count - normalXSortCount);
            }

            for (int i = 0; i < coinSkins.Count && i < result.Count; i++)
            {
                result[i].SetRuntimeSkin(coinSkins[i]);
            }

            return result.ToArray();
        }

        public void CheckXSortListByCfg(ref List<XSortModel> list, int levelIdx, int startIdx)
        {
            //这里判定当前关卡所在是否有配置限定,替换result从startIdx个元素开始
            
            // 如果列表为空或起始索引超出范围，直接返回
            if (list == null || list.Count == 0 || startIdx >= list.Count)
            {
                return;
            }

            // 关卡ID从1开始，所以需要 levelIdx + 1
            int levelId = levelIdx + 1;
            
            // 从配置表中查找当前关卡对应的配置
            var bgCfg = ConfigHelper.Tables.TbGameBgShow.DataList.FirstOrDefault(x => 
                levelId >= x.StartLevel && levelId <= x.EndLevel);

            // 如果没有找到配置或配置中没有XSortIndexInclude，直接返回
            if (bgCfg == null || bgCfg.XSortIndexInclude == null || bgCfg.XSortIndexInclude.Count == 0)
            {
                return;
            }

            // 收集需要替换的XSortData
            List<XSortModel> XSortDataToReplace = new List<XSortModel>();
            foreach (int XSortIndex in bgCfg.XSortIndexInclude)
            {
                XSortModel XSort = GetXSort(XSortIndex);
                // 确保XSortData存在且不是黄金活动棋子
                // XSortIndexInclude中的XSortData必然加入，所以强制替换
                if (XSort != null)
                {
                    XSortDataToReplace.Add(XSort);
                }
            }

            // 如果没有可替换的XSortData，直接返回
            if (XSortDataToReplace.Count == 0)
            {
                return;
            }

            // 计算可以替换的数量（不能超过列表剩余空间）
            int availableReplaceCount = list.Count - startIdx;
            int replaceCount = Mathf.Min(XSortDataToReplace.Count, availableReplaceCount);

            // 从startIdx位置开始替换
            for (int i = 0; i < replaceCount; i++)
            {
                if (startIdx + i < list.Count)
                {
                    list[startIdx + i] = XSortDataToReplace[i];
                }
            }
        }

        public BackgroundModel GetLastAvailableBackgroundData()
        {
            if (BackgroundModel.Length == 0)
                return null;
            return BackgroundModel[2];
        }

        [Button]
        public void AdjustLevelsDifficulty()
        {
            for (int i = 0; i < levels.Length; i++)
            {
                float desiredDifficulty = GetDifficulty(levels[i].SetsAmount, levels[i].AmountOfLayers);

                int elementsRequired = Mathf.RoundToInt(levels[i].SetsAmount / desiredDifficulty);

                ReflectionUtils.InjectInstanceComponent<LevelModel>(levels[i], "elementsPerLevel", elementsRequired);

                RuntimeEditorUtils.SetDirty(levels[i]);
            }

            RuntimeEditorUtils.SetDirty(this);
        }

        private float GetDifficulty(int setsAmount, int layersAmount)
        {
            if (setsAmount < 20 || layersAmount <= 2)
            {
                return Mathf.Round(Random.Range(1f, 1.7f) * 10.0f) * 0.1f;
            }
            else if (setsAmount < 30 || layersAmount <= 3)
            {
                return Mathf.Round(Random.Range(2f, 2.4f) * 10.0f) * 0.1f;
            }
            else if (setsAmount < 40 || layersAmount <= 4)
            {
                return Mathf.Round(Random.Range(2.4f, 3.2f) * 10.0f) * 0.1f;
            }
            else
            {
                return Mathf.Round(Random.Range(3f, 4f) * 10.0f) * 0.1f;
            }
        }
    }
}
