#pragma warning disable 0649

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TileGameCore;
using UnityEngine.Tilemaps;
using cfg.Game;

namespace FishTilePlay
{
    [CreateAssetMenu(menuName = "Content/Level Database/Level Database", fileName = "Level Database")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] LevelModel[] levels;
        public LevelModel[] Levels => levels;

        [SerializeField] TileModel[] tiles;
        public TileModel[] Tiles => tiles;

        [SerializeField] TileEffect[] tileEffects;
        public TileEffect[] TileEffects => tileEffects;

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
            if (tiles != null)
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (tiles[i] != null)
                    {
                        tiles[i].Init(i);
                    }
                }
            }

            if (tileEffects == null) return;

            for (int i = 0; i < tileEffects.Length; i++)
            {
                if (tileEffects[i] == null)
                {
                    Debug.LogError($"LevelCatalog.tileEffects[{i}] is missing. Assign all TileEffect prefabs in Level Database.");
                    continue;
                }

                tileEffects[i].Init();
            }
        }

        public void Unload()
        {
            if (tiles != null)
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i]?.Unload();
                }
            }

            if (tileEffects != null)
            {
                for (int i = 0; i < tileEffects.Length; i++)
                {
                    tileEffects[i]?.Unload();
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

        public TileModel GetTile(int i)
        {
            if (i < tiles.Length && i >= 0)
                return tiles[i];

            return null;
        }

        /// <summary>
        /// 从完整棋子池中挑一个本关未使用、且非 GoldTree 的 TileModel，作为金币匹配载体。
        /// </summary>
        public TileModel PickUnusedTileData(TileModel[] usedTiles, int excludeTileIndex = -1)
        {
            if (tiles == null || tiles.Length == 0)
            {
                return null;
            }

            HashSet<int> usedIndices = new();
            if (usedTiles != null)
            {
                for (int i = 0; i < usedTiles.Length; i++)
                {
                    if (usedTiles[i] != null)
                    {
                        usedIndices.Add(usedTiles[i].TileIndex);
                    }
                }
            }

            usedIndices.Add(TileModel.GoldenActivityTileIdx);
            if (excludeTileIndex >= 0)
            {
                usedIndices.Add(excludeTileIndex);
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                TileModel candidate = tiles[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.CollectionID == 100)
                {
                    continue;
                }

                if (!usedIndices.Contains(candidate.TileIndex))
                {
                    return candidate;
                }
            }

            return null;
        }


        //根据关卡从GameBgShow中拿到TileIndexInclude,这是当前levels获得
        //TileData外显注意  [SerializeField] private int index; //但是不可更改

        //数组中的id是index 必然加入返回的数据中,除非数据量超了.
        //然后进入原来有逻辑获得集合,原有逻辑需要判定ileData中的CollectionId为100的不加入判定.

    
        // public TileModel[] AvailableForLevel(LevelModel level, int dynamicPieceNumber = 0)
        // {
     
        //     int levelIndex = System.Array.FindIndex(levels, x => x == level);
        //     if (levelIndex != -1)
        //     {
        //         return AvailableForLevel(levelIndex, dynamicPieceNumber);
        //     }

        //     return AvailableForLevel(0, dynamicPieceNumber);
        // }

        public TileModel[] AvailableForLevel(LevelModel level, int dynamicPieceNumber)
        {
            return AvailableForLevel(level, dynamicPieceNumber, false);
        }

        public TileModel[] AvailableForLevel(LevelModel level, int dynamicPieceNumber, bool bInGoldenActivity)
        {
            return AvailableForLevel(level, dynamicPieceNumber, bInGoldenActivity, false);
        }

        public TileModel[] AvailableForLevel(LevelModel level, int dynamicPieceNumber, bool bInGoldenActivity, bool isFirstEnter)
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

            return AvailableForLevelFromeCfg(levelIndex, dynamicPieceNumber, bInGoldenActivity, -1, isFirstEnter);
        }


        public TileModel[] AvailableForLevelFromeCfg(int levelIdx, int dynamicPieceNumber = 0, bool bInGoldenActivity = false, int goldenIdx = -1)
        {
            return AvailableForLevelFromeCfg(levelIdx, dynamicPieceNumber, bInGoldenActivity, goldenIdx, false);
        }

        public TileModel[] AvailableForLevelFromeCfg(int levelIdx, int dynamicPieceNumber, bool bInGoldenActivity, int goldenIdx, bool isFirstEnter)
        {
            LevelModel levelData = FetchLevelData(levelIdx);
            if (levelData == null)
            {
                Debug.LogError($"[LevelCatalog] AvailableForLevelFromeCfg: missing LevelModel at index {levelIdx}");
                return System.Array.Empty<TileModel>();
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].ClearRuntimeSkin();
            }

            List<TileModel> result = new();
            // 旧花色资源废弃后，TileData只作为对象池和匹配组载体，开放限制由TilesNew配置控制。
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].TileIndex != TileModel.GoldenActivityTileIdx &&
                    tiles[i].CollectionID != 100
                    )
                {
                    result.Add(tiles[i]);
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
                return AvailableForCoinLevel(levelIdx, levelData, result, bInGoldenActivity);
            }

            //根据关卡的元素限制数量, 从结果列表中随机选择元素
            int elementsPerLevel = Mathf.Clamp(dynamicPieceNumber, 1, result.Count);
            if (result.Count > elementsPerLevel)
            {
                result.RemoveRange(elementsPerLevel, result.Count - elementsPerLevel);
            }

            int replaceStartIdx = 0;
            if (bInGoldenActivity)
            {
                List<TileModel> tempList = new(tiles);
                var oneTileData = tempList.Find((v) => v.TileIndex == TileModel.GoldenActivityTileIdx);
                if (oneTileData != null && result.Count > 0)
                {
                    result[0] = oneTileData;
                    replaceStartIdx = 1;
                }
            }

            int normalTileCount = result.Count - replaceStartIdx;
            int reservedGoldTreeGroupCount = bInGoldenActivity ? 2 : 0;
            int skinBuildCount = bInGoldenActivity ? dynamicPieceNumber : normalTileCount;
            string colorChallenge = LevelDynamicConfigProvider.GetLevelConfig(levelIdx + 1)?.Colorchallenge;
            int levelNumber = levelIdx + 1;
            bool colorEnabled = ActivityData.IsColorChallengeEnabled() && !string.IsNullOrEmpty(colorChallenge);
            int colorSkinTotal = colorEnabled
                ? TileSkinCatalog.CountColorChallengeSkins(colorChallenge, levelNumber)
                : 0;
            bool useColor = colorEnabled && colorSkinTotal >= dynamicPieceNumber;
            LevelDirector.MarkColorChallenge(levelIdx, useColor);
            if (colorEnabled && !useColor)
            {
#if UNITY_EDITOR
                Debug.LogError($"level={levelNumber} color={colorChallenge} 颜色棋子总{colorSkinTotal} 不够配置 {dynamicPieceNumber}");
#endif
            }

            List<TileSkinRuntimeData> levelSkins;
            if (useColor)
            {
                levelSkins = TileSkinCatalog.BuildColorChallengeSkins(
                    colorChallenge,
                    levelNumber,
                    skinBuildCount,
                    bInGoldenActivity ? "yellow" : "",
                    reservedGoldTreeGroupCount
                );
            }
            else
            {
                levelSkins = TileSkinCatalog.BuildLevelSkins(
                    dynamicPieceNumber,
                    levelIdx + 1,
                    skinBuildCount,
                    levelData.GetAmountOfFilledCells(),
                    bInGoldenActivity ? "yellow" : "",
                    reservedGoldTreeGroupCount,
                    isFirstEnter
                );
            }

            if (levelSkins.Count > 0)
            {
                if (levelSkins.Count < normalTileCount)
                {
                    result.RemoveRange(replaceStartIdx + levelSkins.Count, normalTileCount - levelSkins.Count);
                }

                for (int i = 0; i < levelSkins.Count; i++)
                {
                    result[replaceStartIdx + i].SetRuntimeSkin(levelSkins[i]);
                }
            }
            else
            {
                // 新资源配置缺失时保留旧兜底，避免关卡无法生成。
                // Level Editor Test Level also needs this when ConfigHelper skins fail to build.
                CheckTileListByCfg(ref result, levelIdx, replaceStartIdx);
            }


            return result.ToArray();
        }

        TileModel[] AvailableForCoinLevel(int levelIdx, LevelModel levelData, List<TileModel> result, bool bInGoldenActivity)
        {
            int replaceStartIdx = 0;
            if (bInGoldenActivity)
            {
                List<TileModel> tempList = new(tiles);
                var oneTileData = tempList.Find((v) => v.TileIndex == TileModel.GoldenActivityTileIdx);
                if (oneTileData != null && result.Count > 0)
                {
                    result[0] = oneTileData;
                    replaceStartIdx = 1;
                }
            }

            LevelDirector.GetCoinRewardSplit(
                levelData.GetAmountOfFilledCells(),
                bInGoldenActivity,
                out int coinTileCount,
                out int otherGroupCount
            );

            List<TileSkinRuntimeData> coinSkins = TileSkinCatalog.BuildCoinLevelSkins(
                otherGroupCount,
                levelIdx + 1,
                coinTileCount
            );

            int normalTileCount = result.Count - replaceStartIdx;
            if (coinSkins.Count <= 0)
            {
                if (result.Count > replaceStartIdx)
                {
                    result.RemoveRange(replaceStartIdx, result.Count - replaceStartIdx);
                }

                return result.ToArray();
            }

            if (coinSkins.Count < normalTileCount)
            {
                result.RemoveRange(replaceStartIdx + coinSkins.Count, normalTileCount - coinSkins.Count);
            }
            else if (coinSkins.Count > normalTileCount)
            {
                coinSkins.RemoveRange(normalTileCount, coinSkins.Count - normalTileCount);
            }

            for (int i = 0; i < coinSkins.Count && replaceStartIdx + i < result.Count; i++)
            {
                result[replaceStartIdx + i].SetRuntimeSkin(coinSkins[i]);
            }

            return result.ToArray();
        }

        public void CheckTileListByCfg(ref List<TileModel> list, int levelIdx, int startIdx)
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

            // 如果没有找到配置或配置中没有TileIndexInclude，直接返回
            if (bgCfg == null || bgCfg.TileIndexInclude == null || bgCfg.TileIndexInclude.Count == 0)
            {
                return;
            }

            // 收集需要替换的TileData
            List<TileModel> tileDataToReplace = new List<TileModel>();
            foreach (int tileIndex in bgCfg.TileIndexInclude)
            {
                TileModel tile = GetTile(tileIndex);
                // 确保TileData存在且不是黄金活动棋子
                // TileIndexInclude中的TileData必然加入，所以强制替换
                if (tile != null && tile.TileIndex != TileModel.GoldenActivityTileIdx)
                {
                    tileDataToReplace.Add(tile);
                }
            }

            // 如果没有可替换的TileData，直接返回
            if (tileDataToReplace.Count == 0)
            {
                return;
            }

            // 计算可以替换的数量（不能超过列表剩余空间）
            int availableReplaceCount = list.Count - startIdx;
            int replaceCount = Mathf.Min(tileDataToReplace.Count, availableReplaceCount);

            // 从startIdx位置开始替换
            for (int i = 0; i < replaceCount; i++)
            {
                if (startIdx + i < list.Count)
                {
                    list[startIdx + i] = tileDataToReplace[i];
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
