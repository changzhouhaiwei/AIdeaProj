using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TileGameCore;
using Core.Tools.MethodExtension;
using cfg.Game;
using UnityEngine.Tilemaps;
// using UnityEditor.PackageManager;

namespace FishTilePlay
{
    public class LevelGenService
    {
        public class StrategyGenModel
        {
            public int sameColorCnt;
            public int cnt;
        }

        public class LevelDynamicModel
        {
            public string sameColorStr = "";
            public int number;//棋子数量
            public int tileStyleCfg;//TilesNew花色配置
            public int easyHardValue = 0;//0简单聚拢 1艰难打散 3/4/5分层拆组
        }

        private readonly List<TileActor> m_ActTiles;
        public List<TileActor> ActTiles => m_ActTiles;

        /// <summary>棋盘是否已生成完毕（SpawnObjects/SpawnObjectsFromSave 结束后置真），用于在生成完成时锁定 GoldenTree 角标位置。</summary>
        public bool IsBoardSpawned { get; private set; }

        /// <summary>打乱/复活洗牌动画期间为 true：角标可重算但不锁定、且不显示，避免 scale 未到位时锁错位置。</summary>
        public bool IsGoldenTreeCornerLockDeferred { get; private set; }

        private readonly LayersMap layers;
        public LayersMap Layers => layers;

        private string m_dynamicCfgStr;
        public Dictionary<int, TileModel> m_tileGenTypeDic = new();
        public Dictionary<int, bool> m_tileIndexHasDic = new();
        public Dictionary<int, List<StrategyGenModel>> m_sameColorCntDic = new();
        public Dictionary<int, int> m_tileIndexCntDic = new();
        private List<TileSpawnModel> m_emptyTiles = new();
        private LevelDynamicModel m_dynamicData = new();
        private LevelModel level;
        private bool mBNoNetCfg = false;
        private int m_fitToClearTileCnt;//适合去清除Tile的棋子数
        private int m_initialTileCount;

        public int InitialTileCount => m_initialTileCount;
        public int InitialGroupCount => m_initialTileCount / 3;

        public LevelGenService(LevelModel level, GameObject layersParent)
        {
            this.level = level;

            m_ActTiles = new List<TileActor>();
            layers = new LayersMap(level, layersParent);
        }


        public LevelDynamicModel GetDynamicData()
        {
            return m_dynamicData;
        }

        /// <summary>
        /// 将逗号分隔的字符串转换为整数列表
        /// </summary>
        /// <param name="str">格式如 "3,3,3,3" 的字符串</param>
        /// <returns>整数列表</returns>
        public static List<string> ParseStringToList(string str)
        {
            if (string.IsNullOrEmpty(str))
                return new List<string>();

            return str.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    // .Select(int.Parse)
                    .ToList();
        }


        //根据连胜连败次数,决定关卡难度
        public string GetWinFailLevelCfg()
        {
            ResolveWinFailLevelCfg(out _, out string cfgValue);
            return cfgValue;
        }

        public string GetActiveDifficultyDebugLabel()
        {
            string used = m_dynamicCfgStr ?? "";
            ResolveWinFailLevelCfg(out string streakSlot, out string streakCfg);

            string slot = string.IsNullOrEmpty(streakSlot) ? "defaultCfg" : streakSlot;
            if (used != streakCfg)
            {
                string matched = MatchSlotNameByCfg(used);
                if (!string.IsNullOrEmpty(matched))
                    slot = matched;
                else if (!string.IsNullOrEmpty(used))
                    slot = "custom";
            }

            return slot + "；" + used;
        }

        void ResolveWinFailLevelCfg(out string slotName, out string cfgValue)
        {
            ResolveWinFailLevelCfg(GetLoadedLevelConfig(), out slotName, out cfgValue);
        }

        /// <summary>
        /// 当前连胜连败档的组数。取不到时返回 0。
        /// </summary>
        public static int GetActivePieceCount(int levelNumber)
        {
            ResolveWinFailLevelCfg(LevelDynamicConfigProvider.GetLevelConfig(levelNumber), out _, out string cfgValue);
            return ParsePieceCount(cfgValue);
        }

        static int ParsePieceCount(string cfgValue)
        {
            if (string.IsNullOrEmpty(cfgValue))
            {
                return 0;
            }

            if (cfgValue.Contains("number"))
            {
                LevelDynamicModel data = JsonUtility.FromJson<LevelDynamicModel>(cfgValue);
                return data != null ? data.number : 0;
            }

            int comma = cfgValue.IndexOf(',');
            string head = comma >= 0 ? cfgValue.Substring(0, comma) : cfgValue;
            return int.TryParse(head.Trim(), out int pieceCount) ? pieceCount : 0;
        }

        static void ResolveWinFailLevelCfg(GameLevelConfig cfg, out string slotName, out string cfgValue)
        {
            slotName = "defaultCfg";
            cfgValue = "";
            if (cfg == null)
            {
                return;
            }
#if UNITY_EDITOR
            // Level Editor Test Level: use DefaultCfg (new layout / tilesCfg) like a clean start.
            if (GameDirector.AutoRunLevelInEditor)
            {
                cfgValue = cfg.DefaultCfg ?? "";
                return;
            }
#endif
            if (!GameData.BFromStartScene)
                return;

            var userData = GPUserDataManager.Instance.UserData;
            int failCnt = userData.continusFailCnt;
            int winCnt = userData.continusWinCnt;

            if (winCnt == 0 && failCnt == 0)
            {
                slotName = "defaultCfg";
                cfgValue = cfg.DefaultCfg;
            }
            else if (NextDayFirstEnterEase.IsActiveThisLoad && winCnt > 1 && failCnt == 0)
            {
                // 跨日首关无存档：连胜>1 强制 defaultCfg，不影响连胜计数
                slotName = "defaultCfg";
                cfgValue = cfg.DefaultCfg;
            }
            else if (winCnt > 0)
            {
                if (winCnt == 1)
                {
                    slotName = "defaultCfg";
                    cfgValue = cfg.DefaultCfg;
                }
                else if (winCnt <= 2)
                {
                    slotName = "win1";
                    cfgValue = cfg.Win1;
                }
                else if (winCnt <= 4)
                {
                    slotName = "win2";
                    cfgValue = cfg.Win2;
                }
                else if (winCnt <= 6)
                {
                    slotName = "win3";
                    cfgValue = cfg.Win3;
                }
                else
                {
                    slotName = "win4";
                    cfgValue = cfg.Win4;
                }
            }
            else if (failCnt > 0)
            {
                if (failCnt <= 2)
                {
                    slotName = "fail1";
                    cfgValue = cfg.Fail1;
                }
                else if (failCnt <= 4)
                {
                    slotName = "fail2";
                    cfgValue = cfg.Fail2;
                }
                else if (failCnt <= 8)
                {
                    slotName = "fail3";
                    cfgValue = cfg.Fail3;
                }
                else
                {
                    slotName = "fail4";
                    cfgValue = cfg.Fail4;
                }
            }

            if (string.IsNullOrEmpty(cfgValue))
                cfgValue = cfg.DefaultCfg;
        }

        static GameLevelConfig GetLoadedLevelConfig()
        {
            if (GameDirector.levelController == null)
                return null;
            return LevelDynamicConfigProvider.GetLevelConfig(GameDirector.levelController.GetLoadedLevelIndex() + 1);
        }

        static string MatchSlotNameByCfg(string used)
        {
            if (string.IsNullOrEmpty(used))
                return "";

            var cfg = GetLoadedLevelConfig();
            if (cfg == null)
                return "";

            if (used == cfg.Win4) return "win4";
            if (used == cfg.Win3) return "win3";
            if (used == cfg.Win2) return "win2";
            if (used == cfg.Win1) return "win1";
            if (used == cfg.DefaultCfg) return "defaultCfg";
            if (used == cfg.Fail4) return "fail4";
            if (used == cfg.Fail3) return "fail3";
            if (used == cfg.Fail2) return "fail2";
            if (used == cfg.Fail1) return "fail1";
            return "";
        }

        public void ParseDynamicConfig(string str)
        {
            m_dynamicCfgStr = str;

            if (string.IsNullOrEmpty(str))
            {
                m_dynamicData = new LevelDynamicModel
                {
                    easyHardValue = LevelDirector.Level.EasyHardValue,
                    sameColorStr = "",
                    number = LevelDirector.Level.ElementsPerLevel,
                    tileStyleCfg = LevelDirector.Level.ElementsPerLevel
                };
                return;
            }

            if (str.Contains("number"))
            {
                m_dynamicData = JsonUtility.FromJson<LevelDynamicModel>(str);
                if (m_dynamicData.tileStyleCfg <= 0)
                {
                    m_dynamicData.tileStyleCfg = m_dynamicData.number;
                }
                return;
            }
            else
            {
                m_dynamicData = new LevelDynamicModel();
            }

            m_dynamicCfgStr = str;

            var list = ParseStringToList(str);
            m_dynamicData.easyHardValue = LevelDirector.Level.EasyHardValue;
            m_dynamicData.sameColorStr = "";
            m_dynamicData.number = LevelDirector.Level.ElementsPerLevel;
            m_dynamicData.tileStyleCfg = m_dynamicData.number;

            if (list.Count >= 1)
            {
                m_dynamicData.number = int.Parse(list[0]);
                m_dynamicData.tileStyleCfg = m_dynamicData.number;
            }

            if (list.Count >= 2)
            {
                m_dynamicData.easyHardValue = int.Parse(list[1]);
            }

            if (list.Count >= 3)
            {
                var key = list[2].ToInt();
                var oneCfg = LevelDynamicConfigProvider.GetTilesConfig(key);

                if (oneCfg != null)
                {
                    m_dynamicData.sameColorStr = oneCfg.SameColorList;
                }
                else
                {
                    
#if UNITY_EDITOR
                    Debug.LogWarning(string.Format("key {0} 未在 TbGameLevelTilesCfg 配置", key));
#endif
                }
            }
        }

        public string GetDynamicCfgStr()
        {
            return m_dynamicCfgStr;
        }

        public void SetLoadSaveDynamicStr(string savedStr)
        {
            m_dynamicCfgStr = savedStr;
        }

        //根据动态配置获得棋子数量
        public int GetMaxPieceNumber()
        {
            return m_dynamicData.number;
        }

        public int GetTileStyleCfg()
        {
            return m_dynamicData.tileStyleCfg > 0 ? m_dynamicData.tileStyleCfg : m_dynamicData.number;
        }

        /// <summary>
        /// 生成本关卡初始棋子分布，使所有棋子类型尽量均匀分布，避免某种类型过多或过少。
        /// easyHard=3/4/5 时额外保证恰好3子的套数：eh3=5、eh4/eh5=4。
        /// </summary>
        /// <param name="availableTilesData">本关可用的所有棋子类型（TileModel）</param>
        /// <returns>打乱后的棋子类型数组，长度等于本关所有可填充格子的数量</returns>
        private TileModel[] PrepareInitialTiles(TileModel[] availableTilesData)
        {

            // 记录每种棋子类型已分配的数量
            Dictionary<TileModel, int> objectsInLevelAmount = new();

            // 最终要返回的棋子类型分布
            var initialTilesData = new List<TileModel>();

            // 还剩多少格子需要分配棋子
            int tilesDataLeft = level.GetAmountOfFilledCells();
            if (LevelDirector.BInGoldenActivity)
            {
                List<TileModel> tempList = new(availableTilesData);
                var goldTileModel = tempList.Find((v) => v.TileIndex == TileModel.GoldenActivityTileIdx);
                if (goldTileModel == null)
                {
                    Debug.LogError($"[GoldTree] GoldenActivity tile (index {TileModel.GoldenActivityTileIdx}) missing from availableObjects.");
                }
                else
                {
                    // 两连（各 3 颗）共 6 个金树棋子
                    initialTilesData.Add(goldTileModel);
                    initialTilesData.Add(goldTileModel);
                    tilesDataLeft -= 6;
                    tempList.RemoveAll((v) => v.TileIndex == TileModel.GoldenActivityTileIdx);
                    availableTilesData = tempList.ToArray();
                }
            }

            if (LevelDirector.BInCoinRewardLevel && tilesDataLeft > 0)
            {
                TileModel coinTileModel = LevelDirector.Database.PickUnusedTileData(availableTilesData);
                if (coinTileModel == null && availableTilesData != null && availableTilesData.Length > 1)
                {
                    // 池已满时从本关可用类型中拆出一个专作金币，避免无法生成
                    List<TileModel> tempAvailable = new(availableTilesData);
                    coinTileModel = tempAvailable[tempAvailable.Count - 1];
                    tempAvailable.RemoveAt(tempAvailable.Count - 1);
                    availableTilesData = tempAvailable.ToArray();
                }

                if (coinTileModel == null)
                {
                    Debug.LogError("[CoinReward] No TileModel available for coin reward tiles.");
                }
                else
                {
                    coinTileModel.ClearRuntimeSkin();
                    LevelDirector.SetCoinRewardTileData(coinTileModel);

                    LevelDirector.GetCoinRewardSplit(
                        level.GetAmountOfFilledCells(),
                        LevelDirector.BInGoldenActivity,
                        out int coinCount,
                        out _
                    );
                    int coinGroups = coinCount / 3;

                    for (int i = 0; i < coinGroups; i++)
                    {
                        initialTilesData.Add(coinTileModel);
                        tilesDataLeft -= 3;
                    }

                    LevelDirector.SetCoinRewardTileTotalCount(coinCount);
                }

                // 25% 其他棋子每种恰好 1 套，不走 eh 拆组、不复制花色
                if (tilesDataLeft > 0 && availableTilesData != null)
                {
                    foreach (var item in availableTilesData)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (tilesDataLeft < 3)
                        {
                            break;
                        }

                        initialTilesData.Add(item);
                        tilesDataLeft -= 3;
                    }
                }

                if (coinTileModel != null)
                {
                    int extraCoins = 0;
                    while (tilesDataLeft >= 3)
                    {
                        initialTilesData.Add(coinTileModel);
                        tilesDataLeft -= 3;
                        extraCoins += 3;
                    }

                    if (extraCoins > 0)
                    {
                        LevelDirector.SetCoinRewardTileTotalCount(
                            LevelDirector.CoinRewardTileTotalCount + extraCoins
                        );
                    }
                }

                return initialTilesData.OrderBy(x => UnityEngine.Random.value).ToArray();
            }

            int pendingSets = tilesDataLeft / 3;
            int totalSets = initialTilesData.Count + pendingSets;
            int eh = m_dynamicData.easyHardValue;
            // 按配置 eh 值启用，不再用奇偶套数卡死（否则偶数关配置 eh3 会静默失败）
            bool use3 = eh == 3 && totalSets > 5;
            bool use4 = eh == 4 && totalSets > 4;
            bool use5 = eh == 5 && totalSets > 4;
            int exactThree = use3 ? 5 : 4;
            // 金树/金币已占用后，剩余套数必须够铺 exactThree 个单套，否则会多分配套导致空位不够
            if ((use3 || use4 || use5) && pendingSets >= exactThree
                && availableTilesData != null && availableTilesData.Length > 0)
            {
                var ehPrepared = PrepareInitialTilesForEasyHard34(
                    availableTilesData, initialTilesData, tilesDataLeft, exactThree);
                if (ehPrepared != null)
                {
                    return ehPrepared;
                }
            }

            int needTypeCnt = Mathf.RoundToInt(tilesDataLeft / 3);

            //如果需要的类型大于等于棋子类型的数量,先填满  WHW 在原有的基础上保证配置的种类上线起码都有
            if (tilesDataLeft > 0 && availableTilesData != null && availableTilesData.Length > 0)
            {
                if (needTypeCnt >= availableTilesData.Length)
                {
                    //先保证每种都有
                    foreach (var item in availableTilesData)
                    {
                        initialTilesData.Add(item);
                        tilesDataLeft -= 3;
                        objectsInLevelAmount[item] = 1;
                    }
                }
                else
                { //如果需要的类型小于配置的类型
                    int curCnt = 0;
                    foreach (var item in availableTilesData)
                    {
                        initialTilesData.Add(item);
                        tilesDataLeft -= 3;
                        objectsInLevelAmount[item] = 1;
                        if (++curCnt >= needTypeCnt)
                        {
                            break;
                        }
                    }
                }
            }

            // 循环分配，直到所有格子都分配完
            while (tilesDataLeft > 0)
            {
                TileModel tileData = SelectDuplicateTileData(availableTilesData, objectsInLevelAmount);
                if (tileData == null)
                {
                    break;
                }

                if (!objectsInLevelAmount.ContainsKey(tileData))
                {
                    objectsInLevelAmount[tileData] = 0;
                }

                objectsInLevelAmount[tileData]++;

                // 添加到最终分布
                initialTilesData.Add(tileData);

                // 每次消除3个（假设三消），所以每次分配3个棋子
                tilesDataLeft -= 3;
            }


            // 最后打乱分布，避免出现明显的分组或规律
            return initialTilesData.OrderBy(x => UnityEngine.Random.value).ToArray();
        }

        private TileModel SelectDuplicateTileData(TileModel[] availableTilesData, Dictionary<TileModel, int> objectsInLevelAmount)
        {
            if (availableTilesData == null || availableTilesData.Length == 0)
            {
                return null;
            }

            List<TileModel> candidates = availableTilesData
                .Where(tileData => tileData != null && objectsInLevelAmount.ContainsKey(tileData))
                .ToList();
            if (candidates.Count == 0)
            {
                return availableTilesData.GetRandomItem();
            }

            int minAmount = candidates.Min(tileData => objectsInLevelAmount[tileData]);
            Dictionary<string, int> selectedColorCounts = availableTilesData
                .Select(tileData => tileData?.RuntimeSkin?.Color)
                .Where(color => !string.IsNullOrEmpty(color))
                .GroupBy(color => color)
                .ToDictionary(group => group.Key, group => group.Count());

            return candidates
                .Where(tileData => objectsInLevelAmount[tileData] == minAmount)
                .OrderByDescending(tileData => GetDuplicateSelectedColorCount(tileData, selectedColorCounts))
                .ThenBy(GetDuplicateColorPriority)
                .ThenBy(GetDuplicateColorOrder)
                .ThenBy(_ => UnityEngine.Random.value)
                .FirstOrDefault();
        }

        /// <summary>
        /// easyHard=3/4/5：保证恰好3子（类型仅1套）的套数精确为 exactThreeTileSetCount；
        /// 其余套只分给其他类型且每类至少2套，避免多出恰好3子的套。
        /// </summary>
        private TileModel[] PrepareInitialTilesForEasyHard34(
            TileModel[] availableTilesData,
            List<TileModel> initialTilesData,
            int tilesDataLeft,
            int exactThreeTileSetCount)
        {
            int totalSets = tilesDataLeft / 3;
            var reservedIndexes = new HashSet<int>(
                initialTilesData.Where(t => t != null).Select(t => t.TileIndex));

            var shuffledTypes = availableTilesData != null
                ? availableTilesData
                    .Where(t => t != null && !reservedIndexes.Contains(t.TileIndex))
                    .OrderBy(_ => UnityEngine.Random.value)
                    .ToList()
                : new List<TileModel>();

            if (shuffledTypes.Count < exactThreeTileSetCount)
            {
                return null;
            }

            var specialTypes = shuffledTypes.Take(exactThreeTileSetCount).ToList();
            var otherTypes = shuffledTypes.Skip(specialTypes.Count).ToList();
            int remainingSets = totalSets - specialTypes.Count;
            if (remainingSets < 0)
            {
                remainingSets = 0;
            }

            // 先在临时列表凑齐，成功后再写入，避免失败时污染预留列表
            var pendingAdds = new List<TileModel>(specialTypes);

            if (remainingSets > 0)
            {
                if (otherTypes.Count == 0)
                {
                    return null;
                }

                int maxOtherTypes = Mathf.Min(otherTypes.Count, remainingSets / 2);
                if (maxOtherTypes <= 0)
                {
                    return null;
                }

                var usedOthers = otherTypes.Take(maxOtherTypes).ToList();
                int allocated = 0;
                foreach (var t in usedOthers)
                {
                    pendingAdds.Add(t);
                    pendingAdds.Add(t);
                    allocated += 2;
                }

                int leftover = remainingSets - allocated;
                for (int i = 0; i < leftover; i++)
                    pendingAdds.Add(usedOthers[UnityEngine.Random.Range(0, usedOthers.Count)]);
            }

            initialTilesData.AddRange(pendingAdds);
            return initialTilesData.OrderBy(x => UnityEngine.Random.value).ToArray();
        }

        private int GetDuplicateSelectedColorCount(TileModel tileData, Dictionary<string, int> selectedColorCounts)
        {
            string color = tileData?.RuntimeSkin?.Color;
            if (string.IsNullOrEmpty(color))
            {
                return 0;
            }

            return selectedColorCounts.TryGetValue(color, out int count) ? count : 0;
        }

        private int GetDuplicateColorPriority(TileModel tileData)
        {
            return TileSkinCatalog.GetRuntimeColorPriority(tileData?.RuntimeSkin?.Color);
        }

        private int GetDuplicateColorOrder(TileModel tileData)
        {
            return TileSkinCatalog.GetRuntimeColorOrder(tileData?.RuntimeSkin?.Color);
        }

        public void RelinkTiles(bool updateClickableStates = true)
        {
            foreach (LayerBoard layer in layers.Layers)
            {
                int width = layer.Width;
                int height = layer.Height;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        layer[x, y].LinkTile(null);
                    }
                }
            }

            foreach (TileActor tile in m_ActTiles)
            {
                ElementPosition elementPosition = tile.ElementPosition;
                if (updateClickableStates)
                {
                    tile.SetIsClickable(true);
                }

                layers[elementPosition] = tile; //老代码不行啊,这里都没有置上,依托.
                layers.Layers[elementPosition.LayerId][elementPosition].LinkTile(tile);
            }

            if (updateClickableStates)
            {
                UpdateClickableStates(true);
            }
        }

        public void RefreshStackOccludedEffectsAfterBoardChange()
        {
            Physics.SyncTransforms();
            RefreshStackOccludedEffectVisibility();
        }

        public void UpdateClickableStates(bool withAnimation = false)
        {
            BoxCoverManager.UpdateStates();
            AwardColorEggCoverManager.UpdateStates();

            foreach (TileActor tile in m_ActTiles)
            {
                tile.SetIsClickable(IsTileUnconcealed(tile), withAnimation);
            }

            RefreshStackOccludedEffectVisibility();
        }

        public void RefreshStackOccludedEffectVisibility()
        {
            foreach (TileActor tile in m_ActTiles)
            {
                if (tile.Effect is IStackOccludedTileEffect occludedEffect)
                {
                    occludedEffect.RefreshVisibility();
                }
            }
        }

        /// <summary>棋盘重排（如打乱道具）后，解冻所有 GoldenTree 角标落点，使其在下一次刷新时按新布局重新决定。</summary>
        public void ResetGoldenTreeCornerDecisions()
        {
            foreach (TileActor tile in m_ActTiles)
            {
                if (tile != null && tile.Effect is GoldenTreeEffect goldenTreeEffect)
                {
                    goldenTreeEffect.ResetDecidedCornerIndex();
                }
            }
        }

        /// <summary>打乱动画开始前调用：解冻角标并延迟锁定/显示，直至 <see cref="FinalizeGoldenTreeCornerDecisions"/>。</summary>
        public void BeginGoldenTreeCornerRecalculation()
        {
            IsGoldenTreeCornerLockDeferred = true;
            ResetGoldenTreeCornerDecisions();
        }

        /// <summary>打乱动画结束后调用：解除延迟，按最终布局锁定并显示角标。</summary>
        public void FinalizeGoldenTreeCornerDecisions()
        {
            IsGoldenTreeCornerLockDeferred = false;
            RefreshStackOccludedEffectsAfterBoardChange();
        }


        public int GetCalucuteFitTileCnt(TileActor excptBBehaviour)
        {
            m_fitToClearTileCnt = 0;
            CalculateFitTileCnt(excptBBehaviour);
            return m_fitToClearTileCnt;
        }

        //判定dock
        public void CalculateFitTileCnt(TileActor excptBBehaviour = null)
        {
            //1.判定dock区域是否有两个相同棋子
            //2.actTile中当前状态可点击的棋子中能和dock中两个相同的组成三消的数量.
            //3.m_fitToClearTileCnt设置数量.
            m_fitToClearTileCnt = 0;

            // 获取dock中所有已占用的槽位
            var dock = LevelDirector.Dock;
            if (dock == null) return;

            int occupiedSlotsCount = dock.GetOccupiedSlotsCount();
            if (occupiedSlotsCount < 1) return;
            if (occupiedSlotsCount >= LevelDirector.DockAb.GetMaxShowSlotCnt()) return;

            // 获取dock中所有棋子，按类型分组
            var dockTiles = new List<IDockable>();
            for (int i = 0; i < occupiedSlotsCount; i++)
            {
                // 这里需要通过DockBehavior的slots来获取棋子
                // 由于slots是private的，我们需要通过其他方式获取
                // 使用GetHintSlots方法来获取dock中的棋子
                var hintSlots = DockActor.GetHintSlots();
                if (hintSlots != null && hintSlots.Count > 0)
                {
                    dockTiles = hintSlots;
                    break;
                }
            }

            if (dockTiles.Count < 2) return;


            // 检查是否有两个相同类型的棋子
            var tileGroups = dockTiles.GroupBy(tile => tile.UniqueElementID).ToList();
            var twoSameTypeGroups = tileGroups.Where(group => group.Count() >= 1).ToList();

            if (twoSameTypeGroups.Count == 0) return;

            // 对于每个有两个相同棋子的类型，计算actTile中可点击的相同类型棋子数量
            foreach (var group in twoSameTypeGroups)
            {
                var dockTileType = group.First();

                // 如果传入的排除棋子类型与当前dock棋子类型相同，则跳过
                if (excptBBehaviour != null && excptBBehaviour.IsSameType(dockTileType))
                {
                    continue;
                }

                int dockCount = group.Count();
                int neededCount = 3 - dockCount; // 需要多少个actTile中的棋子来组成三消

                if (neededCount <= 0) continue; // 已经有三消了

                // 统计actTile中可点击的相同类型棋子数量
                int clickableCount = 0;
                foreach (var actTile in m_ActTiles)
                {
                    if (actTile.IsClickable && actTile.IsSameType(dockTileType))
                    {
                        clickableCount++;
                    }
                }

                // 计算可以形成的三消数量
                int possibleMatches = clickableCount;
                m_fitToClearTileCnt += possibleMatches;
            }
        }

        public void RemoveObject(TileActor tile)
        {
            m_ActTiles.Remove(tile);

            layers[tile.ElementPosition] = null;
        }

        public void AddObject(TileActor tile)
        {
            if (tile == null)
                return;
            if (!m_ActTiles.Contains(tile))
                m_ActTiles.Add(tile);

            layers[tile.ElementPosition] = tile;
        }

        /// <summary> 棋子在棋盘上的层节点（与 <see cref="SpawnTile"/> 里 SetParent 一致），用于从 Dock 返回前恢复层级。 </summary>
        public Transform GetTileLayerTransform(ElementPosition elementPosition)
        {
            return layers[elementPosition.LayerId].LayerObject.transform;
        }

        public int GetRestTileCount()
        {
            int count = 0;
            foreach (var item in m_ActTiles)
            {
                if (!item.IsSubmitted)
                {
                    count++;
                }
            }
            return count;
        }

        public int GetRestLayerCount()
        {
            Dictionary<int, bool> curDic = new();
            foreach (var item in m_ActTiles)
            {
                curDic[item.LayerID] = true;
            }
            return curDic.Count;
        }

        //真正创建棋子的方法
        private TileActor SpawnTile(TileModel tileData, ElementPosition elementPosition)
        {
            TileActor tile = tileData.Pool.GetPooledObject().GetComponent<TileActor>();
            // 确保从内存池获取的棋子对象是激活的，避免关卡切换时棋子不显示的问题
            if (!tile.gameObject.activeSelf)
            {
                tile.gameObject.SetActive(true);
            }
            tile.DisableEffect();
            tile.Init(tileData, elementPosition);
            if (LevelDirector.IsCoinRewardTile(tileData))
            {
                tile.ApplyCoinRewardSkin();
            }
            tile.transform.SetParent(layers[elementPosition.LayerId].LayerObject.transform);
            var pos = LevelFitter.GetPosition(tile.ElementPosition);
            // pos.z = 0;
            tile.transform.localPosition = pos;
            tile.transform.localScale = Vector3.one;
            tile.SetScale(LevelFitter.TileSize);
            layers[tile.ElementPosition] = tile;
            m_ActTiles.Add(tile);

            return tile;
        }



        public void SpawnObjects(TileModel[] availableObjects)
        {
            // 关卡资源上的 sameColorListStr 已废弃，不参与摆棋。同色只来自难度配置第三段。
            string strrr = m_dynamicData.sameColorStr;

            if (!string.IsNullOrEmpty(strrr))//后续都走这个,所有配置都OK的情况下
            {
                SpawnObjectsNew(availableObjects);
            }
            else
            {
                SpawnObjectsOld(availableObjects);
            }

            // 同步物理，确保后续 GoldenTree 角标定位读取到正确的 BoxCollider.bounds（否则遮挡检测失败会全部落到默认角）。
            Physics.SyncTransforms();
            IsBoardSpawned = true;
            CacheInitialTileCount();
        }

        public void CacheInitialTileCount()
        {
            m_initialTileCount = m_ActTiles.Count;
        }

        public void SetInitialTileCount(int count)
        {
            m_initialTileCount = count;
        }




        //收集level的空格
        private void CollectEmptyTiles()
        {
            m_emptyTiles.Clear();
            // 遍历所有层级，收集需要填充棋子的位置
            for (int i = 0; i < level.AmountOfLayers; i++) //从高到低
            {
                Layer layer = level.GetLayer(i);

                // 判断当前层级是偶数层还是奇数层（用于确定层级大小）
                bool isEven = (level.AmountOfLayers - i - 1) % 2 == 0;

                // 根据层级类型获取对应的大小
                Vector2Int size = isEven ? LevelDirector.EvenLayerSize : LevelDirector.OddLayerSize;

                // 从下到上、从左到右遍历层级中的每个位置
                for (int y = size.y - 1; y >= 0; y--)
                {
                    for (int x = 0; x < size.x; x++)
                    {
                        CellModel cellData = layer[y].GetCell(x);

                        // 如果该位置需要填充棋子
                        if (cellData.IsFilled)
                        {
                            // 创建棋子生成数据
                            TileSpawnModel tileSpawnData = new()
                            {
                                ElementPosition = new ElementPosition(x, y, i),  // 设置棋子位置
                                CellModel = cellData,                             // 设置单元格数据
                                LayerIndex = i,                                  // 设置层级索引
                                Layer = layer,                                   // 设置层级引用
                                LayerSize = size                                // 设置层级大小
                            };

                            m_emptyTiles.Add(tileSpawnData);
                        }
                    }
                }
            }
        }

        int mgoldenTileNumber = 0;
        readonly HashSet<int> m_goldenTreeX2TileIndices = new();

        public void PrepareGoldenTreeX2Plan(int levelIndex)
        {
            m_goldenTreeX2TileIndices.Clear();
            if (!LevelDirector.BInGoldenActivity)
            {
                return;
            }

            int x2Count = GameData.GoldTreeActData.RollGoldenTreeX2Count(levelIndex);
            int[] indices = { 0, 1, 2, 3, 4, 5 };
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            for (int i = 0; i < x2Count; i++)
            {
                m_goldenTreeX2TileIndices.Add(indices[i]);
            }
        }

        static void ApplyGoldenTreeEffect(TileActor tile, bool isX2)
        {
            TileEffect effect = LevelDirector.GetTileEffect(TileEffectType.GoldenTree);
            if (effect == null)
            {
                return;
            }

            effect.ApplyEffect(tile);
            if (tile.Effect != null)
            {
                tile.Effect.SetEffectData(GoldenTreeEffect.SerializeState(isX2));
            }
        }

        void ApplyCellEffect(TileActor tile, CellModel cellData)
        {
            if (cellData.Effect == TileEffectType.None)
            {
                return;
            }

            if (cellData.Effect == TileEffectType.GoldenTree)
            {
                ApplyGoldenTreeEffect(tile, cellData.GoldenTreeIsX2);
                return;
            }

            TileEffect effect = LevelDirector.GetTileEffect(cellData);
            if (effect != null)
            {
                effect.ApplyEffect(tile);
            }
        }

        void CheckSetGoldenTreeData(TileModel tileData, TileSpawnModel spawnData)
        {
            if (mgoldenTileNumber >= 6)
            {
                return;
            }

            if (tileData.TileIndex == TileModel.GoldenActivityTileIdx && LevelDirector.BInGoldenActivity)
            {
                spawnData.CellModel.Effect = TileEffectType.GoldenTree;
                spawnData.CellModel.GoldenTreeIsX2 = m_goldenTreeX2TileIndices.Contains(mgoldenTileNumber);
                mgoldenTileNumber++;
            }

            if (LevelDirector.BInGoldenActivity != true && spawnData.CellModel.Effect == TileEffectType.GoldenTree)
            {
                spawnData.CellModel.Effect = TileEffectType.None;
                spawnData.CellModel.GoldenTreeIsX2 = false;
            }
        }

        ///老的棋子分布规则
        private void SpawnObjectsOld(TileModel[] availableObjects)
        {
            mgoldenTileNumber = 0;
            // 准备初始棋子类型分布（确保每种棋子类型都有合适的数量）
            TileModel[] initialTilesData = PrepareInitialTiles(availableObjects);
            // 收集所有需要应用特效的棋子数据
            List<EffectModel> effectDatas = new();
            // 收集所有需要放置棋子的空位置信息
            List<TileSpawnModel> emptyTiles = new();
            CollectEmptyTiles();
            emptyTiles = m_emptyTiles;

            if (!TrySpawnWithEasyHard34(initialTilesData, emptyTiles, effectDatas))
            {
                // eh3/4/5 失败时不要把 3/4/5 当 hard 打散权重
                int fallbackEh = m_dynamicData.easyHardValue == 1 ? 1 : 0;
                for (int i = 0; i < initialTilesData.Length; i++)
                {
                    SpawnSetWithWeight(initialTilesData[i], 3, emptyTiles, fallbackEh, effectDatas);
                }
            }

            // 应用所有收集的特效
            foreach (EffectModel effectData in effectDatas)
            {
                if (effectData.EffectType == TileEffectType.GoldenTree)
                {
                    ApplyGoldenTreeEffect(effectData.TileActor, effectData.GoldenTreeIsX2);
                    continue;
                }

                TileEffect effect = LevelDirector.GetTileEffect(effectData.EffectType);
                if (effect != null)
                {
                    effect.ApplyEffect(effectData.TileActor);
                }
            }
        }

        private void SpawnObjectsNew(TileModel[] availableObjects)
        {
            mgoldenTileNumber = 0;
            // 准备初始棋子类型分布（确保每种棋子类型都有合适的数量）
            TileModel[] initialTilesData = PrepareInitialTiles(availableObjects);//所有分配的棋子*3 

            m_tileGenTypeDic.Clear(); //itemIdx对应的TileModel
            m_tileIndexHasDic.Clear(); //
            m_sameColorCntDic.Clear();
            m_tileIndexCntDic.Clear();//每个itemIdx最终会生成多少棋子

            //初始化所有的Tile类型
            foreach (var item in initialTilesData)
            {
                m_tileGenTypeDic[item.TileIndex] = item;

                if (m_tileIndexCntDic.TryGetValue(item.TileIndex, out int v)) //如果有+3,没有设置3
                {
                    m_tileIndexCntDic[item.TileIndex] += 3;
                }
                else
                {
                    m_tileIndexCntDic[item.TileIndex] = 3;
                }
            }

            if (LevelDirector.BInGoldenActivity
                && m_tileIndexCntDic.TryGetValue(TileModel.GoldenActivityTileIdx, out int goldCnt))
            {
                m_tileIndexCntDic[TileModel.GoldenActivityTileIdx] = Mathf.Min(6, goldCnt);
            }

            m_emptyTiles.Clear();
            CollectEmptyTiles();

            // eh3/4/5 与同色策略冲突：策略会先占末层，导致底子拆组无法落地。
            // 配置为分层拆组时，优先整盘走 eh；失败再回退同色策略。
            int eh = m_dynamicData.easyHardValue;
            if (eh == 3 || eh == 4 || eh == 5)
            {
                if (TrySpawnEasyHardOnFullBoard())
                {
                    return;
                }

                Debug.LogWarning(
                    $"[LevelGen] eh={eh} 整盘拆组未生效，回退同色策略。sets={m_tileIndexCntDic.Values.Sum() / 3}, empty={m_emptyTiles.Count}");
            }

            string sameColorStr = m_dynamicData.sameColorStr ?? "";
            m_sameColorCntDic = ParseStrategyGenData(sameColorStr);

            int totalLayers = LevelDirector.Level.AmountOfLayers;
            for (int i = 0; i < totalLayers; i++)
            {
                GenOneLayerByStategy(i, ref initialTilesData);
            }

            GenRestEmptyTiles2();
        }

        /// <summary>
        /// 在同色策略之前，用当前全部待生成套数尝试 eh3/4/5 整盘放置。
        /// 成功则清空计数并返回 true；失败不改 empty/计数，返回 false。
        /// </summary>
        private bool TrySpawnEasyHardOnFullBoard()
        {
            var sets = new List<TileModel>();
            var leftovers = new List<(TileModel tileData, int count)>();
            var typeTotalTileCount = new Dictionary<int, int>();

            foreach (var item in m_tileIndexCntDic)
            {
                if (item.Value <= 0)
                    continue;
                if (!m_tileGenTypeDic.TryGetValue(item.Key, out TileModel tileData))
                    continue;

                typeTotalTileCount[item.Key] = item.Value;
                int fullSets = item.Value / 3;
                int rem = item.Value % 3;
                for (int i = 0; i < fullSets; i++)
                    sets.Add(tileData);
                if (rem > 0)
                    leftovers.Add((tileData, rem));
            }

            if (!TrySpawnWithEasyHard34(sets.ToArray(), m_emptyTiles, null, typeTotalTileCount))
            {
                return false;
            }

            foreach (var leftover in leftovers)
                SpawnSetWithWeight(leftover.tileData, leftover.count, m_emptyTiles, 0, null);

            m_tileIndexCntDic.Clear();
            return true;
        }

        //根据策略生成一层
        private void GenOneLayerByStategy(int layerIdx, ref TileModel[] initialTilesData)
        {
            m_tileIndexHasDic.Clear();

            //如果有设置该层限定
            //本层所有空位
            var oneList = m_emptyTiles.Where(tile => tile.LayerIndex == layerIdx).ToList();
            var oneRandomList = oneList.OrderBy(x => UnityEngine.Random.value).ToList();//打乱分布

            if (m_sameColorCntDic.TryGetValue(layerIdx, out List<StrategyGenModel> sList))
            {
                foreach (var item in sList)
                {
                    int sameColorNum = item.sameColorCnt;//几连色
                    int cnt = item.cnt;//几套
                    //开始取棋子
                    for (int i = 0; i < cnt; i++)
                    {
                        int tileIdx = GetRandomTileIndex(sameColorNum); //做了去重处理
                        if (tileIdx >= 0)
                        {
                            m_tileIndexHasDic[tileIdx] = true;
                            if (oneRandomList.Count > 0)
                            {
                                SpawnTileToDstList(tileIdx, ref oneRandomList, sameColorNum);
                            }
                            //放入对应位置
                        }
                    }
                }
            }

            Debug.Log("总共数量:是  " + m_tileIndexHasDic.Keys.Count);
        }

        public void SpawnObjectsFromSave(RecoverLevelData rlData)
        {
            m_tileGenTypeDic.Clear(); // itemIdx -> TileModel
            m_tileIndexHasDic.Clear();
            m_sameColorCntDic.Clear();
            m_tileIndexCntDic.Clear();

            if (rlData != null)
            {
                var tilesArr = LevelDirector.Database.Tiles;
                bool BindCoinFromList(List<RecoverLevelData.RLATileData> list)
                {
                    if (list == null)
                    {
                        return false;
                    }

                    foreach (var item in list)
                    {
                        if (!LevelDirector.IsCoinRewardSkin(item.tileSkinName))
                        {
                            continue;
                        }

                        if (item.tileIndex >= 0 && item.tileIndex < tilesArr.Length)
                        {
                            LevelDirector.SetCoinRewardTileData(tilesArr[item.tileIndex]);
                            return true;
                        }
                    }

                    return false;
                }

                if (!BindCoinFromList(rlData.AllTileDataList))
                {
                    BindCoinFromList(rlData.DockTileIndices);
                }
            }

            // 逐个还原保存的棋子
            foreach (var item in rlData.AllTileDataList)
            {
                // 检查层级合法
                if (item.layer < 0 || item.layer >= level.AmountOfLayers)
                    continue;

                // 计算该层大小并检查坐标合法
                bool isEven = (level.AmountOfLayers - item.layer - 1) % 2 == 0;
                Vector2Int size = isEven ? LevelDirector.EvenLayerSize : LevelDirector.OddLayerSize;
                if (item.x < 0 || item.x >= size.x || item.y < 0 || item.y >= size.y)
                    continue;

                // 该位置必须是可填充格
                Layer targetLayer = level.GetLayer(item.layer);
                CellModel cellData = targetLayer[item.y].GetCell(item.x);
                if (!cellData.IsFilled)
                    continue;

                var tilesArr = LevelDirector.Database.Tiles;
                TileModel tileData = tilesArr[item.tileIndex];

                // 放置棋子
                ElementPosition pos = new ElementPosition(item.x, item.y, item.layer);
                var spawnedTile = SpawnTile(tileData, pos);
                if (LevelDirector.IsCoinRewardSkin(item.tileSkinName)
                    || LevelDirector.IsCoinRewardTile(tileData))
                {
                    spawnedTile.ApplyCoinRewardSkin();
                    if (LevelDirector.CoinRewardTileData == null)
                    {
                        LevelDirector.SetCoinRewardTileData(tileData);
                    }
                }
                else
                {
                    spawnedTile.UpdateRuntimeSkin(item.tileSkinName);
                }

                if (item.isFission)
                    spawnedTile.SetFission(true);

                if (item.effectType == TileEffectType.GoldenTree && !LevelDirector.BInGoldenActivity)
                {
                }
                else if (item.effectType == TileEffectType.GoldenTree)
                {
                    ApplyGoldenTreeEffect(spawnedTile, GoldenTreeEffect.ParseIsX2(item.effectData));
                }
                else if (item.effectType != TileEffectType.None
                    && !string.IsNullOrEmpty(item.effectData)
                    && item.effectType != TileEffectType.Link)
                {
                    TileEffect effect = LevelDirector.GetTileEffect(item.effectType);
                    if (effect != null)
                    {
                        effect.ApplyEffect(spawnedTile);
                        spawnedTile.Effect.SetEffectData(item.effectData);
                    }
                }
            }

            SetLoadSaveDynamicStr(rlData.dynamicStr);

            // 同步物理，确保后续 GoldenTree 角标定位读取到正确的 BoxCollider.bounds（否则遮挡检测失败会全部落到默认角）。
            Physics.SyncTransforms();
            IsBoardSpawned = true;

            if (rlData.initialTileCount > 0)
                SetInitialTileCount(rlData.initialTileCount);
            else
                SetInitialTileCount(level.GetAmountOfFilledCells());

            if (rlData.coinRewardTotalCount > 0)
            {
                LevelDirector.SetCoinRewardTileTotalCount(rlData.coinRewardTotalCount);
            }
        }

        //生成
        private void SpawnTileToDstList(int tileIndex, ref List<TileSpawnModel> dstList, int num)
        {
            if (m_tileGenTypeDic.TryGetValue(tileIndex, out TileModel tData))
            {
                int idx = 0;

                for (int i = dstList.Count - 1; i >= 0; i--)
                {
                    var item = dstList[i];
                    if (idx < num)
                    {
                        var spawned = SpawnTile(tData, item.ElementPosition);
                        if (spawned == null)
                        {
                            continue;
                        }

                        CheckSetGoldenTreeData(tData, item);
                        // 应用该格的特效（与旧逻辑一致）
                        if (item.CellModel.Effect != TileEffectType.None)
                        {
                            ApplyCellEffect(spawned, item.CellModel);
                            // 复位运行时注入的 GoldenTree 效果，避免污染共享关卡数据导致下次进关 x2 累积
                            item.CellModel.CheckResetEffect();
                        }
                        MinusItemIndexCnt(tileIndex, 1);
                        m_emptyTiles.Remove(item);
                        dstList.RemoveAt(i);
                    }
                    idx++;
                }
            }
        }

        private void GenRestEmptyTiles2()
        {
            var emptyTiles = m_emptyTiles;
            var sets = new List<TileModel>();
            var leftovers = new List<(TileModel tileData, int count)>();
            // 记录拆分前各类型剩余子数，供 eh 判定「恰好3子」（避免 Value=4/5 被拆成 1套+零头后误判）
            var typeTotalTileCount = new Dictionary<int, int>();

            foreach (var item in m_tileIndexCntDic.ToList())
            {
                if (item.Value <= 0)
                    continue;

                if (!m_tileGenTypeDic.TryGetValue(item.Key, out TileModel tileData))
                    continue;

                typeTotalTileCount[item.Key] = item.Value;

                int fullSets = item.Value / 3;
                int rem = item.Value % 3;
                for (int i = 0; i < fullSets; i++)
                    sets.Add(tileData);
                if (rem > 0)
                    leftovers.Add((tileData, rem));

                m_tileIndexCntDic[item.Key] = 0;
            }

            bool usedEasyHard34 = TrySpawnWithEasyHard34(sets.ToArray(), emptyTiles, null, typeTotalTileCount);
            // eh3/4/5 失败时不要把 3/4/5 当 hard 打散权重（RecalculateWeight 非0即 hard）
            int restEasyHard = usedEasyHard34 ? 0
                : (m_dynamicData.easyHardValue == 1 ? 1 : 0);

            if (!usedEasyHard34)
            {
                foreach (var set in sets)
                    SpawnSetWithWeight(set, 3, emptyTiles, restEasyHard, null);
            }

            foreach (var leftover in leftovers)
                SpawnSetWithWeight(leftover.tileData, leftover.count, emptyTiles, restEasyHard, null);
        }

        /// <summary>
        /// easyHard=3：sets 奇数且&gt;5，从恰好3子的套中取5套拆组；
        /// 底2子优先铺满末层、不够再到倒数第二层；固定1子从「底子占用最深倒数层+2」起逐套+1。
        /// easyHard=4：sets 偶数且&gt;4，从恰好3子的套中取4套：
        /// 底子同样优先末层再倒数第二层；底放1则剩余2子固定到倒数4/5，底放2则剩余1子固定到倒数6/7。
        /// easyHard=5：同 eh4 底部分，固定层对调——底放2则剩余1子到倒数4/5，底放1则剩余2子到倒数6/7。
        /// 其余套按 easyHard=0 聚拢。条件不满足返回 false。
        /// </summary>
        /// <param name="typeTotalTileCount">各类型剩余总子数；为 null 时用 sets 内出现次数×3 判断恰好3子</param>
        private bool TrySpawnWithEasyHard34(
            TileModel[] sets,
            List<TileSpawnModel> emptyTiles,
            List<EffectModel> effectDatas,
            Dictionary<int, int> typeTotalTileCount = null)
        {
            if (sets == null || sets.Length == 0 || emptyTiles == null)
                return false;

            int eh = m_dynamicData.easyHardValue;
            int setsAmount = sets.Length;
            // 按配置 eh 启用，不再要求套数奇偶（偶数关配 eh3 也应生效）
            bool use3 = eh == 3 && setsAmount > 5;
            bool use4 = eh == 4 && setsAmount > 4;
            bool use5 = eh == 5 && setsAmount > 4;
            if (!use3 && !use4 && !use5)
                return false;

            // 恰好3子：该类型总共只剩3子（1套）；eh3=5，eh4/eh5=4
            int specialNeed = use3 ? 5 : 4;
            var typeSetCount = new Dictionary<int, int>();
            foreach (var s in sets)
            {
                if (s == null)
                    continue;
                typeSetCount.TryGetValue(s.TileIndex, out int cnt);
                typeSetCount[s.TileIndex] = cnt + 1;
            }

            bool IsExactThreeTiles(TileModel s)
            {
                if (s == null)
                    return false;
                if (typeTotalTileCount != null)
                {
                    return typeTotalTileCount.TryGetValue(s.TileIndex, out int total) && total == 3;
                }
                return typeSetCount.TryGetValue(s.TileIndex, out int c) && c == 1;
            }

            var exactThreeTileSets = sets
                .Where(IsExactThreeTiles)
                .GroupBy(s => s.TileIndex)
                .Select(g => g.First())
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            if (exactThreeTileSets.Count < specialNeed)
                return false;

            var special = exactThreeTileSets.Take(specialNeed).ToList();
            var specialIndexes = new HashSet<int>(special.Select(s => s.TileIndex));
            var rest = sets
                .Where(s => s != null && !specialIndexes.Contains(s.TileIndex))
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            if (use3)
            {
                // eh3：5套均为底2子 + 1子固定层
                // 底子占用最深倒数层：末层空位>=10 → 1，否则最多占末两层 → 2
                // 上1子起始 = bottomDepth + 2（末层够10时从倒数第3层起），再逐套 +1
                int[] bottomCounts = { 2, 2, 2, 2, 2 };
                int fixedStartFromBottom = GetEh3FixedStartFromBottom(emptyTiles, specialNeed * 2);

                for (int i = 0; i < special.Count; i++)
                {
                    SpawnSpecialSplitSet(special[i], bottomCounts[i], 1, fixedStartFromBottom + i, emptyTiles, effectDatas);
                }
            }
            else if (use4)
            {
                // eh4：最后两层 2+2+1+1；
                // 底放1 → 剩余2子固定到倒数4或5；底放2 → 剩余1子固定到倒数6或7
                var eh4Plans = new[]
                {
                    (bottom: 1, fixedCnt: 2, fromBottom: 4),
                    (bottom: 1, fixedCnt: 2, fromBottom: 5),
                    (bottom: 2, fixedCnt: 1, fromBottom: 6),
                    (bottom: 2, fixedCnt: 1, fromBottom: 7),
                }.OrderBy(_ => UnityEngine.Random.value).ToArray();

                for (int i = 0; i < special.Count; i++)
                {
                    var plan = eh4Plans[i];
                    SpawnSpecialSplitSet(special[i], plan.bottom, plan.fixedCnt, plan.fromBottom, emptyTiles, effectDatas);
                }
            }
            else // use5
            {
                // eh5：最后两层 2+2+1+1（同 eh4）；
                // 底放2 → 剩余1子固定到倒数4或5；底放1 → 剩余2子固定到倒数6或7
                var eh5Plans = new[]
                {
                    (bottom: 2, fixedCnt: 1, fromBottom: 4),
                    (bottom: 2, fixedCnt: 1, fromBottom: 5),
                    (bottom: 1, fixedCnt: 2, fromBottom: 6),
                    (bottom: 1, fixedCnt: 2, fromBottom: 7),
                }.OrderBy(_ => UnityEngine.Random.value).ToArray();

                for (int i = 0; i < special.Count; i++)
                {
                    var plan = eh5Plans[i];
                    SpawnSpecialSplitSet(special[i], plan.bottom, plan.fixedCnt, plan.fromBottom, emptyTiles, effectDatas);
                }
            }

            foreach (var set in rest)
            {
                SpawnSetWithWeight(set, 3, emptyTiles, 0, effectDatas);
            }

            return true;
        }

        /// <summary>
        /// eh3 上1子起始倒数层 = 底子占用最深倒数层数 + 2。
        /// 底子规则：优先末层，不够再到倒数第二层；故末层空位够 bottomTileCount 时深度为1（起始倒数第3），否则为2（起始倒数第4）。
        /// </summary>
        private int GetEh3FixedStartFromBottom(List<TileSpawnModel> emptyTiles, int bottomTileCount)
        {
            int lastLayer = level.AmountOfLayers - 1;
            int lastAvail = emptyTiles == null
                ? 0
                : emptyTiles.Count(t => t.LayerIndex == lastLayer);
            int bottomDepth = lastAvail >= bottomTileCount ? 1 : 2;
            return bottomDepth + 2;
        }

        /// <summary>
        /// 特殊套：底子优先末层、不够再到倒数第二层（可跨这两层）；
        /// fixedCount 子落到固定倒数层（整组同层，放不下整组下一层）；剩余 easy 聚拢。
        /// </summary>
        private void SpawnSpecialSplitSet(TileModel tileData, int bottomCount, int fixedCount, int fixedFromBottom, List<TileSpawnModel> emptyTiles, List<EffectModel> effectDatas)
        {
            int placed = 0;

            // 底子：优先铺满最后一层，不够再落到倒数第二层（不要求同组同层）
            var bottomTiles = TakeEmptyTilesPreferLastTwoLayers(emptyTiles, bottomCount);
            foreach (var spawnData in bottomTiles)
            {
                SpawnTileAt(tileData, spawnData, effectDatas);
                placed++;
            }

            // 剩余固定层：整组同层，目标层放不下则下一层
            var fixedTiles = TakeEmptyTilesOnFromBottomSameLayer(emptyTiles, fixedFromBottom, fixedCount);
            foreach (var spawnData in fixedTiles)
            {
                SpawnTileAt(tileData, spawnData, effectDatas);
                placed++;
            }

            int remaining = 3 - placed;
            if (remaining > 0)
                SpawnSetWithWeight(tileData, remaining, emptyTiles, 0, effectDatas);
        }

        /// <summary>
        /// 底子取位：优先从末层逐个取满，末层不够再到倒数第二层；不要求同组同层。
        /// </summary>
        private List<TileSpawnModel> TakeEmptyTilesPreferLastTwoLayers(List<TileSpawnModel> emptyTiles, int count)
        {
            var result = new List<TileSpawnModel>();
            if (count <= 0 || emptyTiles == null || emptyTiles.Count == 0)
                return result;

            void TakeFromLayer(int layerIndex)
            {
                if (layerIndex < 0 || result.Count >= count)
                    return;

                var candidates = emptyTiles.Where(t => t.LayerIndex == layerIndex)
                    .OrderBy(_ => UnityEngine.Random.value).ToList();
                foreach (var t in candidates)
                {
                    if (result.Count >= count)
                        break;
                    result.Add(t);
                    emptyTiles.Remove(t);
                }
            }

            int lastLayer = level.AmountOfLayers - 1;
            int secondLast = level.AmountOfLayers - 2;
            TakeFromLayer(lastLayer);
            TakeFromLayer(secondLast);
            return result;
        }

        /// <summary>
        /// 在倒数第 fromBottom 层整组取 count 空位；本层不够则整组放到下一层（倒数+1），保证同组同层。
        /// </summary>
        private List<TileSpawnModel> TakeEmptyTilesOnFromBottomSameLayer(List<TileSpawnModel> emptyTiles, int fromBottom, int count)
        {
            var result = new List<TileSpawnModel>();
            if (count <= 0 || emptyTiles == null || emptyTiles.Count == 0)
                return result;

            for (int fb = fromBottom; fb <= level.AmountOfLayers; fb++)
            {
                int layerIndex = level.AmountOfLayers - fb;
                if (layerIndex < 0)
                    break;

                var candidates = emptyTiles.Where(t => t.LayerIndex == layerIndex)
                    .OrderBy(_ => UnityEngine.Random.value).ToList();
                if (candidates.Count < count)
                    continue;

                for (int i = 0; i < count; i++)
                {
                    result.Add(candidates[i]);
                    emptyTiles.Remove(candidates[i]);
                }
                return result;
            }

            return result;
        }

        private void SpawnTileAt(TileModel tileData, TileSpawnModel spawnData, List<EffectModel> effectDatas)
        {
            TileActor behavior = SpawnTile(tileData, spawnData.ElementPosition);
            CheckSetGoldenTreeData(tileData, spawnData);

            if (spawnData.CellModel.Effect == TileEffectType.None)
                return;

            if (effectDatas != null)
            {
                effectDatas.Add(new EffectModel(behavior, spawnData.CellModel.Effect, spawnData.CellModel.GoldenTreeIsX2));
                spawnData.CellModel.CheckResetEffect();
            }
            else
            {
                ApplyCellEffect(behavior, spawnData.CellModel);
                spawnData.CellModel.CheckResetEffect();
            }
        }

        /// <summary>按权重放置同类型 tileCount 枚棋子（先底层随机首子，再权重选其余）。</summary>
        private void SpawnSetWithWeight(TileModel tileData, int tileCount, List<TileSpawnModel> emptyTiles, int easyHardValue, List<EffectModel> effectDatas)
        {
            if (tileData == null || tileCount <= 0 || emptyTiles == null || emptyTiles.Count == 0)
                return;

            TileSpawnModel firstTileSpawnData = emptyTiles.OrderBy(x => UnityEngine.Random.value)
                .OrderBy(x => x.LayerIndex).FirstOrDefault();
            if (firstTileSpawnData == null)
                return;

            emptyTiles.Remove(firstTileSpawnData);
            SpawnTileAt(tileData, firstTileSpawnData, effectDatas);

            float totalWeight = 0;
            foreach (TileSpawnModel emptyTile in emptyTiles)
            {
                emptyTile.RecalculateWeight(firstTileSpawnData.LayerIndex, easyHardValue);
                totalWeight += emptyTile.RandomWeight;
            }

            for (int a = 0; a < tileCount - 1; a++)
            {
                if (emptyTiles.Count == 0)
                    break;

                TileSpawnModel selectedTileData = null;
                if (totalWeight > 0)
                {
                    float randomValue = UnityEngine.Random.Range(0, totalWeight);
                    float currentWeight = 0;
                    foreach (TileSpawnModel emptyTile in emptyTiles)
                    {
                        currentWeight += emptyTile.RandomWeight;
                        if (currentWeight >= randomValue)
                        {
                            selectedTileData = emptyTile;
                            break;
                        }
                    }
                }

                if (selectedTileData == null)
                    selectedTileData = emptyTiles[UnityEngine.Random.Range(0, emptyTiles.Count)];

                emptyTiles.Remove(selectedTileData);
                totalWeight -= selectedTileData.RandomWeight;
                SpawnTileAt(tileData, selectedTileData, effectDatas);
            }
        }

        private void MinusItemIndexCnt(int tileIndex, int minusCnt)
        {
            if (m_tileIndexCntDic.TryGetValue(tileIndex, out int v))
            {
                m_tileIndexCntDic[tileIndex] -= minusCnt;
            }
        }


        //随机获得一个剩余数量大于cnt的tileIndex
        private int GetRandomTileIndex(int needNum)
        {
            foreach (var item in m_tileIndexCntDic)
            {
                if (item.Value >= needNum && !m_tileIndexHasDic.ContainsKey(item.Key))
                {
                    return item.Key;
                }
            }

            return -1;
        }

        public List<TileActor> GetSortedTiles()
        {
            List<TileActor> sortedTiles = new();
            for (int i = level.AmountOfLayers - 1; i >= 0; i--)
            {
                Layer layer = level.GetLayer(i);

                bool isEven = (level.AmountOfLayers - i - 1) % 2 == 0;

                var size = isEven ? LevelDirector.EvenLayerSize : LevelDirector.OddLayerSize;

                for (int y = size.y - 1; y >= 0; y--)
                {
                    for (int x = 0; x < size.x; x++)
                    {
                        CellModel cellData = layer[y].GetCell(x);

                        if (cellData.IsFilled)
                        {
                            sortedTiles.Add(layers[i][x, y]);
                        }
                    }
                }
            }

            return sortedTiles;
        }

        public bool IsThereAnyTilesOnLayer(int layerId)
        {
            var size = ((level.AmountOfLayers - layerId - 1) % 2) == 0 ? LevelDirector.EvenLayerSize : LevelDirector.OddLayerSize;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    if (layers[new ElementPosition(x, y, layerId)].State)
                        return true;
                }
            }

            return false;
        }


        /// <summary>WHW
        /// 判断指定位置的棋子是否可见（未被遮挡）
        /// 该方法用于确定多层关卡中棋子是否应该显示给玩家
        /// </summary>
        /// <param name="tilePos">要检查的棋子位置</param>
        /// <returns>如果棋子可见返回true，否则返回false</returns>
        public bool IsTileUnconcealed(TileActor tileBehavior)
        {
            var tilePos = tileBehavior.ElementPosition;
            // 最顶层（LayerId = 0）的棋子总是可见的
            if (tilePos.LayerId == 0)
                return true;
            return IsTileUnconcealedNew(tileBehavior);
        }

        public bool IsTileUnconcealedNew(TileActor tileBehavior)
        {
            var tilePos = tileBehavior.ElementPosition;

            // 最顶层（LayerId = 0）的棋子总是可见的
            if (tilePos.LayerId == 0)
                return true;

            if (BoxCoverManager.IsTileBlocked(tileBehavior)
                || AwardColorEggCoverManager.IsTileBlocked(tileBehavior))
                return false;

            // 从当前层级向下检查所有底层，看是否有棋子遮挡了当前棋子 ,i小的再上方
            for (int i = tilePos.LayerId - 1; i >= 0; i--)
            {
                var layerGrid = layers.GetLayerGrid(i);
                for (int x = 0; x < layerGrid.Width; x++)
                {
                    for (int y = 0; y < layerGrid.Height; y++)
                    {
                        var levelItem = layerGrid[x, y];
                        if (TileStackUtil.IsCurrentUpperBlocker(
                                tileBehavior, levelItem.Tile))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool IsTileExists(ElementPosition elementPosition)
        {
            int layerId = elementPosition.LayerId;
            int width = layers[layerId].Width;
            int height = layers[layerId].Height;

            if (elementPosition.X >= 0 && elementPosition.X < width && elementPosition.Y >= 0 && elementPosition.Y < height)
            {
                return layers[elementPosition].State;
            }

            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < m_ActTiles.Count; i++)
            {
                m_ActTiles[i].Clear();
            }

            m_ActTiles.Clear();
            layers.Clear();
        }


        //解析动态字符串,棋子生成位置
        private Dictionary<int, List<StrategyGenModel>> ParseStrategyGenData(string str)
        {
            Dictionary<int, List<StrategyGenModel>> dic = new Dictionary<int, List<StrategyGenModel>>();
            string[] groups = str.Split(',');

            for (int i = 0; i < groups.Length; i++)
            {
                List<StrategyGenModel> list = new List<StrategyGenModel>();
                string group = groups[i];

                if (!string.IsNullOrEmpty(group))
                {
                    string[] items = group.Split('|');
                    foreach (var item in items)
                    {
                        string[] parts = item.Split('_');
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0], out int sameColorCnt) &&
                            int.TryParse(parts[1], out int cnt))
                        {
                            list.Add(new StrategyGenModel
                            {
                                sameColorCnt = sameColorCnt,
                                cnt = cnt
                            });
                        }
                    }
                }

                dic[i] = list;
            }

            return dic;
        }

        private class EffectModel
        {
            public TileActor TileActor;
            public TileEffectType EffectType;
            public bool GoldenTreeIsX2;

            public EffectModel(TileActor tileBehavior, TileEffectType effectType, bool goldenTreeIsX2 = false)
            {
                TileActor = tileBehavior;
                EffectType = effectType;
                GoldenTreeIsX2 = goldenTreeIsX2;
            }
        }

        /// <summary>
        /// 棋子生成数据类，用于存储棋子生成位置的相关信息和权重
        /// </summary>
        private class TileSpawnModel
        {
            /// <summary>
            /// 棋子在层级中的位置坐标
            /// </summary>
            public ElementPosition ElementPosition;

            /// <summary>
            /// 该位置的单元格数据，包含特效等信息
            /// </summary>
            public CellModel CellModel;

            /// <summary>
            /// 棋子所在的层级索引（从0开始）
            /// </summary>
            public int LayerIndex;

            /// <summary>
            /// 棋子所在的层级对象
            /// </summary>
            public Layer Layer;

            /// <summary>
            /// 当前层级的尺寸（宽度和高度）
            /// </summary>
            public Vector2Int LayerSize;

            /// <summary>
            /// 随机权重值，用于权重随机选择算法
            /// 权重越高，被选中的概率越大
            /// </summary>
            public float RandomWeight;

            /// <summary>
            /// 重新计算权重值
            /// 权重计算原理：基于与基准层级的距离差，距离越近权重越高
            /// 
            /// 权重计算公式：RandomWeight = Mathf.Clamp(3 - layerDiff, 0, int.MaxValue)
            /// - layerDiff = 当前层级索引 - 基准层级索引
            /// - 当层级差为0时（同一层级），权重为3
            /// - 当层级差为1时（相邻层级），权重为2
            /// - 当层级差为2时，权重为1
            /// - 当层级差>=3时，权重为0（不会被选中）
            /// 
            /// 设计目的：
            /// 1. 优先选择与第一个棋子相同或相邻层级的位置
            /// 2. 确保相同类型的棋子聚集在相近的层级
            /// 3. 避免棋子分布过于分散，提高游戏体验
            /// </summary>
            /// <param name="baseLayerIndex">基准层级索引，通常是第一个棋子的层级</param>
            public void RecalculateWeightEasy(int baseLayerIndex)
            {
                // 计算当前层级与基准层级的距离差
                int layerDiff = LayerIndex - baseLayerIndex;
                // 权重计算：3减去层级差，确保距离越近权重越高
                // 使用Clamp确保权重在合理范围内（0到int.MaxValue）
                RandomWeight = Mathf.Clamp(3 - layerDiff, 0, int.MaxValue);
            }

            public void RecalculateWeightHard(int baseLayerIndex)
            {
                // 计算当前层级与基准层级的距离差（取绝对值）
                int layerDiff = Mathf.Abs(LayerIndex - baseLayerIndex);
                // 权重计算：层级差加1，确保距离越远权重越高
                // 使用Clamp确保权重在合理范围内（1到int.MaxValue）
                RandomWeight = Mathf.Clamp(layerDiff + 1, 1, int.MaxValue);
            }

            public void RecalculateWeight(int baseLayerIndex, int easyHardValue = 0)
            {
                if (easyHardValue == 0)
                {
                    RecalculateWeightEasy(baseLayerIndex);
                }
                else
                {
                    RecalculateWeightHard(baseLayerIndex);
                }
            }
        }

    }



    //通用方法 
}