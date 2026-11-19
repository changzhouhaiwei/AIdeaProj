using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XSortGameCore;
using Core.Tools.MethodExtension;
using cfg.Game;
using UnityEngine.Tilemaps;
// using UnityEditor.PackageManager;

namespace FishXSortPlay
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
            public int XSortStyleCfg;//XSortsNew花色配置
            public int easyHardValue = 0;//0简单聚拢 1艰难打散 3/4/5分层拆组
        }

        private readonly List<XSortActor> m_ActXSorts;
        public List<XSortActor> ActXSorts => m_ActXSorts;

        /// <summary>棋盘是否已生成完毕（SpawnObjects/SpawnObjectsFromSave 结束后置真）。</summary>
        public bool IsBoardSpawned { get; private set; }

        private readonly LayersMap layers;
        public LayersMap Layers => layers;

        private string m_dynamicCfgStr;
        public Dictionary<int, XSortModel> m_XSortGenTypeDic = new();
        public Dictionary<int, bool> m_XSortIndexHasDic = new();
        public Dictionary<int, List<StrategyGenModel>> m_sameColorCntDic = new();
        public Dictionary<int, int> m_XSortIndexCntDic = new();
        private List<XSortSpawnModel> m_emptyXSorts = new();
        private LevelDynamicModel m_dynamicData = new();
        private LevelModel level;
        private bool mBNoNetCfg = false;
        private int m_fitToClearXSortCnt;//适合去清除XSort的棋子数
        private int m_initialXSortCount;

        public int InitialXSortCount => m_initialXSortCount;
        public int InitialGroupCount => m_initialXSortCount / 3;

        public LevelGenService(LevelModel level, GameObject layersParent)
        {
            this.level = level;

            m_ActXSorts = new List<XSortActor>();
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
            // Level Editor Test Level: use DefaultCfg (new layout / XSortsCfg) like a clean start.
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
                    XSortStyleCfg = LevelDirector.Level.ElementsPerLevel
                };
                return;
            }

            if (str.Contains("number"))
            {
                m_dynamicData = JsonUtility.FromJson<LevelDynamicModel>(str);
                if (m_dynamicData.XSortStyleCfg <= 0)
                {
                    m_dynamicData.XSortStyleCfg = m_dynamicData.number;
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
            m_dynamicData.XSortStyleCfg = m_dynamicData.number;

            if (list.Count >= 1)
            {
                m_dynamicData.number = int.Parse(list[0]);
                m_dynamicData.XSortStyleCfg = m_dynamicData.number;
            }

            if (list.Count >= 2)
            {
                m_dynamicData.easyHardValue = int.Parse(list[1]);
            }

            if (list.Count >= 3)
            {
                var key = list[2].ToInt();
                var oneCfg = LevelDynamicConfigProvider.GetXSortsConfig(key);

                if (oneCfg != null)
                {
                    m_dynamicData.sameColorStr = oneCfg.SameColorList;
                }
                else
                {
                    
#if UNITY_EDITOR
                    Debug.LogWarning(string.Format("key {0} 未在 TbGameLevelXSortsCfg 配置", key));
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

        public int GetXSortStyleCfg()
        {
            return m_dynamicData.XSortStyleCfg > 0 ? m_dynamicData.XSortStyleCfg : m_dynamicData.number;
        }

        /// <summary>
        /// 生成本关卡初始棋子分布，使所有棋子类型尽量均匀分布，避免某种类型过多或过少。
        /// easyHard=3/4/5 时额外保证恰好3子的套数：eh3=5、eh4/eh5=4。
        /// </summary>
        /// <param name="availableXSortsData">本关可用的所有棋子类型（XSortModel）</param>
        /// <returns>打乱后的棋子类型数组，长度等于本关所有可填充格子的数量</returns>
        private XSortModel[] PrepareInitialXSorts(XSortModel[] availableXSortsData)
        {

            // 记录每种棋子类型已分配的数量
            Dictionary<XSortModel, int> objectsInLevelAmount = new();

            // 最终要返回的棋子类型分布
            var initialXSortsData = new List<XSortModel>();

            // 还剩多少格子需要分配棋子
            int XSortsDataLeft = level.GetAmountOfFilledCells();

            if (LevelDirector.BInCoinRewardLevel && XSortsDataLeft > 0)
            {
                XSortModel coinXSortModel = LevelDirector.Database.PickUnusedXSortData(availableXSortsData);
                if (coinXSortModel == null && availableXSortsData != null && availableXSortsData.Length > 1)
                {
                    // 池已满时从本关可用类型中拆出一个专作金币，避免无法生成
                    List<XSortModel> tempAvailable = new(availableXSortsData);
                    coinXSortModel = tempAvailable[tempAvailable.Count - 1];
                    tempAvailable.RemoveAt(tempAvailable.Count - 1);
                    availableXSortsData = tempAvailable.ToArray();
                }

                if (coinXSortModel == null)
                {
                    Debug.LogError("[CoinReward] No XSortModel available for coin reward XSorts.");
                }
                else
                {
                    coinXSortModel.ClearRuntimeSkin();
                    LevelDirector.SetCoinRewardXSortData(coinXSortModel);

                    LevelDirector.GetCoinRewardSplit(
                        level.GetAmountOfFilledCells(),
                        out int coinCount,
                        out _
                    );
                    int coinGroups = coinCount / 3;

                    for (int i = 0; i < coinGroups; i++)
                    {
                        initialXSortsData.Add(coinXSortModel);
                        XSortsDataLeft -= 3;
                    }

                    LevelDirector.SetCoinRewardXSortTotalCount(coinCount);
                }

                // 25% 其他棋子每种恰好 1 套，不走 eh 拆组、不复制花色
                if (XSortsDataLeft > 0 && availableXSortsData != null)
                {
                    foreach (var item in availableXSortsData)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (XSortsDataLeft < 3)
                        {
                            break;
                        }

                        initialXSortsData.Add(item);
                        XSortsDataLeft -= 3;
                    }
                }

                if (coinXSortModel != null)
                {
                    int extraCoins = 0;
                    while (XSortsDataLeft >= 3)
                    {
                        initialXSortsData.Add(coinXSortModel);
                        XSortsDataLeft -= 3;
                        extraCoins += 3;
                    }

                    if (extraCoins > 0)
                    {
                        LevelDirector.SetCoinRewardXSortTotalCount(
                            LevelDirector.CoinRewardXSortTotalCount + extraCoins
                        );
                    }
                }

                return initialXSortsData.OrderBy(x => UnityEngine.Random.value).ToArray();
            }

            int pendingSets = XSortsDataLeft / 3;
            int totalSets = initialXSortsData.Count + pendingSets;
            int eh = m_dynamicData.easyHardValue;
            // 按配置 eh 值启用，不再用奇偶套数卡死（否则偶数关配置 eh3 会静默失败）
            bool use3 = eh == 3 && totalSets > 5;
            bool use4 = eh == 4 && totalSets > 4;
            bool use5 = eh == 5 && totalSets > 4;
            int exactThree = use3 ? 5 : 4;
            // 金树/金币已占用后，剩余套数必须够铺 exactThree 个单套，否则会多分配套导致空位不够
            if ((use3 || use4 || use5) && pendingSets >= exactThree
                && availableXSortsData != null && availableXSortsData.Length > 0)
            {
                var ehPrepared = PrepareInitialXSortsForEasyHard34(
                    availableXSortsData, initialXSortsData, XSortsDataLeft, exactThree);
                if (ehPrepared != null)
                {
                    return ehPrepared;
                }
            }

            int needTypeCnt = Mathf.RoundToInt(XSortsDataLeft / 3);

            //如果需要的类型大于等于棋子类型的数量,先填满  WHW 在原有的基础上保证配置的种类上线起码都有
            if (XSortsDataLeft > 0 && availableXSortsData != null && availableXSortsData.Length > 0)
            {
                if (needTypeCnt >= availableXSortsData.Length)
                {
                    //先保证每种都有
                    foreach (var item in availableXSortsData)
                    {
                        initialXSortsData.Add(item);
                        XSortsDataLeft -= 3;
                        objectsInLevelAmount[item] = 1;
                    }
                }
                else
                { //如果需要的类型小于配置的类型
                    int curCnt = 0;
                    foreach (var item in availableXSortsData)
                    {
                        initialXSortsData.Add(item);
                        XSortsDataLeft -= 3;
                        objectsInLevelAmount[item] = 1;
                        if (++curCnt >= needTypeCnt)
                        {
                            break;
                        }
                    }
                }
            }

            // 循环分配，直到所有格子都分配完
            while (XSortsDataLeft > 0)
            {
                XSortModel XSortData = SelectDuplicateXSortData(availableXSortsData, objectsInLevelAmount);
                if (XSortData == null)
                {
                    break;
                }

                if (!objectsInLevelAmount.ContainsKey(XSortData))
                {
                    objectsInLevelAmount[XSortData] = 0;
                }

                objectsInLevelAmount[XSortData]++;

                // 添加到最终分布
                initialXSortsData.Add(XSortData);

                // 每次消除3个（假设三消），所以每次分配3个棋子
                XSortsDataLeft -= 3;
            }


            // 最后打乱分布，避免出现明显的分组或规律
            return initialXSortsData.OrderBy(x => UnityEngine.Random.value).ToArray();
        }

        private XSortModel SelectDuplicateXSortData(XSortModel[] availableXSortsData, Dictionary<XSortModel, int> objectsInLevelAmount)
        {
            if (availableXSortsData == null || availableXSortsData.Length == 0)
            {
                return null;
            }

            List<XSortModel> candidates = availableXSortsData
                .Where(XSortData => XSortData != null && objectsInLevelAmount.ContainsKey(XSortData))
                .ToList();
            if (candidates.Count == 0)
            {
                return availableXSortsData.GetRandomItem();
            }

            int minAmount = candidates.Min(XSortData => objectsInLevelAmount[XSortData]);
            Dictionary<string, int> selectedColorCounts = availableXSortsData
                .Select(XSortData => XSortData?.RuntimeSkin?.Color)
                .Where(color => !string.IsNullOrEmpty(color))
                .GroupBy(color => color)
                .ToDictionary(group => group.Key, group => group.Count());

            return candidates
                .Where(XSortData => objectsInLevelAmount[XSortData] == minAmount)
                .OrderByDescending(XSortData => GetDuplicateSelectedColorCount(XSortData, selectedColorCounts))
                .ThenBy(GetDuplicateColorPriority)
                .ThenBy(GetDuplicateColorOrder)
                .ThenBy(_ => UnityEngine.Random.value)
                .FirstOrDefault();
        }

        /// <summary>
        /// easyHard=3/4/5：保证恰好3子（类型仅1套）的套数精确为 exactThreeXSortSetCount；
        /// 其余套只分给其他类型且每类至少2套，避免多出恰好3子的套。
        /// </summary>
        private XSortModel[] PrepareInitialXSortsForEasyHard34(
            XSortModel[] availableXSortsData,
            List<XSortModel> initialXSortsData,
            int XSortsDataLeft,
            int exactThreeXSortSetCount)
        {
            int totalSets = XSortsDataLeft / 3;
            var reservedIndexes = new HashSet<int>(
                initialXSortsData.Where(t => t != null).Select(t => t.XSortIndex));

            var shuffledTypes = availableXSortsData != null
                ? availableXSortsData
                    .Where(t => t != null && !reservedIndexes.Contains(t.XSortIndex))
                    .OrderBy(_ => UnityEngine.Random.value)
                    .ToList()
                : new List<XSortModel>();

            if (shuffledTypes.Count < exactThreeXSortSetCount)
            {
                return null;
            }

            var specialTypes = shuffledTypes.Take(exactThreeXSortSetCount).ToList();
            var otherTypes = shuffledTypes.Skip(specialTypes.Count).ToList();
            int remainingSets = totalSets - specialTypes.Count;
            if (remainingSets < 0)
            {
                remainingSets = 0;
            }

            // 先在临时列表凑齐，成功后再写入，避免失败时污染预留列表
            var pendingAdds = new List<XSortModel>(specialTypes);

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

            initialXSortsData.AddRange(pendingAdds);
            return initialXSortsData.OrderBy(x => UnityEngine.Random.value).ToArray();
        }

        private int GetDuplicateSelectedColorCount(XSortModel XSortData, Dictionary<string, int> selectedColorCounts)
        {
            string color = XSortData?.RuntimeSkin?.Color;
            if (string.IsNullOrEmpty(color))
            {
                return 0;
            }

            return selectedColorCounts.TryGetValue(color, out int count) ? count : 0;
        }

        private int GetDuplicateColorPriority(XSortModel XSortData)
        {
            return XSortSkinCatalog.GetRuntimeColorPriority(XSortData?.RuntimeSkin?.Color);
        }

        private int GetDuplicateColorOrder(XSortModel XSortData)
        {
            return XSortSkinCatalog.GetRuntimeColorOrder(XSortData?.RuntimeSkin?.Color);
        }

        public void RelinkXSorts(bool updateClickableStates = true)
        {
            foreach (LayerBoard layer in layers.Layers)
            {
                int width = layer.Width;
                int height = layer.Height;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        layer[x, y].LinkXSort(null);
                    }
                }
            }

            foreach (XSortActor XSort in m_ActXSorts)
            {
                ElementPosition elementPosition = XSort.ElementPosition;
                if (updateClickableStates)
                {
                    XSort.SetIsClickable(true);
                }

                layers[elementPosition] = XSort; //老代码不行啊,这里都没有置上,依托.
                layers.Layers[elementPosition.LayerId][elementPosition].LinkXSort(XSort);
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

            foreach (XSortActor XSort in m_ActXSorts)
            {
                XSort.SetIsClickable(IsXSortUnconcealed(XSort), withAnimation);
            }

            RefreshStackOccludedEffectVisibility();
        }

        public void RefreshStackOccludedEffectVisibility()
        {
            foreach (XSortActor XSort in m_ActXSorts)
            {
                if (XSort.Effect is IStackOccludedXSortEffect occludedEffect)
                {
                    occludedEffect.RefreshVisibility();
                }
            }
        }

        public int GetCalucuteFitXSortCnt(XSortActor excptBBehaviour)
        {
            m_fitToClearXSortCnt = 0;
            CalculateFitXSortCnt(excptBBehaviour);
            return m_fitToClearXSortCnt;
        }

        //判定dock
        public void CalculateFitXSortCnt(XSortActor excptBBehaviour = null)
        {
            //1.判定dock区域是否有两个相同棋子
            //2.actXSort中当前状态可点击的棋子中能和dock中两个相同的组成三消的数量.
            //3.m_fitToClearXSortCnt设置数量.
            m_fitToClearXSortCnt = 0;

            // 获取dock中所有已占用的槽位
            var dock = LevelDirector.Dock;
            if (dock == null) return;

            int occupiedSlotsCount = dock.GetOccupiedSlotsCount();
            if (occupiedSlotsCount < 1) return;
            if (occupiedSlotsCount >= LevelDirector.DockAb.GetMaxShowSlotCnt()) return;

            // 获取dock中所有棋子，按类型分组
            var dockXSorts = new List<IDockable>();
            for (int i = 0; i < occupiedSlotsCount; i++)
            {
                // 这里需要通过DockBehavior的slots来获取棋子
                // 由于slots是private的，我们需要通过其他方式获取
                // 使用GetHintSlots方法来获取dock中的棋子
                var hintSlots = DockActor.GetHintSlots();
                if (hintSlots != null && hintSlots.Count > 0)
                {
                    dockXSorts = hintSlots;
                    break;
                }
            }

            if (dockXSorts.Count < 2) return;


            // 检查是否有两个相同类型的棋子
            var XSortGroups = dockXSorts.GroupBy(XSort => XSort.UniqueElementID).ToList();
            var twoSameTypeGroups = XSortGroups.Where(group => group.Count() >= 1).ToList();

            if (twoSameTypeGroups.Count == 0) return;

            // 对于每个有两个相同棋子的类型，计算actXSort中可点击的相同类型棋子数量
            foreach (var group in twoSameTypeGroups)
            {
                var dockXSortType = group.First();

                // 如果传入的排除棋子类型与当前dock棋子类型相同，则跳过
                if (excptBBehaviour != null && excptBBehaviour.IsSameType(dockXSortType))
                {
                    continue;
                }

                int dockCount = group.Count();
                int neededCount = 3 - dockCount; // 需要多少个actXSort中的棋子来组成三消

                if (neededCount <= 0) continue; // 已经有三消了

                // 统计actXSort中可点击的相同类型棋子数量
                int clickableCount = 0;
                foreach (var actXSort in m_ActXSorts)
                {
                    if (actXSort.IsClickable && actXSort.IsSameType(dockXSortType))
                    {
                        clickableCount++;
                    }
                }

                // 计算可以形成的三消数量
                int possibleMatches = clickableCount;
                m_fitToClearXSortCnt += possibleMatches;
            }
        }

        public void RemoveObject(XSortActor XSort)
        {
            m_ActXSorts.Remove(XSort);

            layers[XSort.ElementPosition] = null;
        }

        public void AddObject(XSortActor XSort)
        {
            if (XSort == null)
                return;
            if (!m_ActXSorts.Contains(XSort))
                m_ActXSorts.Add(XSort);

            layers[XSort.ElementPosition] = XSort;
        }

        /// <summary> 棋子在棋盘上的层节点（与 <see cref="SpawnXSort"/> 里 SetParent 一致），用于从 Dock 返回前恢复层级。 </summary>
        public Transform GetXSortLayerTransform(ElementPosition elementPosition)
        {
            return layers[elementPosition.LayerId].LayerObject.transform;
        }

        public int GetRestXSortCount()
        {
            int count = 0;
            foreach (var item in m_ActXSorts)
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
            foreach (var item in m_ActXSorts)
            {
                curDic[item.LayerID] = true;
            }
            return curDic.Count;
        }

        //真正创建棋子的方法
        private XSortActor SpawnXSort(XSortModel XSortData, ElementPosition elementPosition)
        {
            XSortActor XSort = XSortData.Pool.GetPooledObject().GetComponent<XSortActor>();
            // 确保从内存池获取的棋子对象是激活的，避免关卡切换时棋子不显示的问题
            if (!XSort.gameObject.activeSelf)
            {
                XSort.gameObject.SetActive(true);
            }
            XSort.DisableEffect();
            XSort.Init(XSortData, elementPosition);
            if (LevelDirector.IsCoinRewardXSort(XSortData))
            {
                XSort.ApplyCoinRewardSkin();
            }
            XSort.transform.SetParent(layers[elementPosition.LayerId].LayerObject.transform);
            var pos = LevelFitter.GetPosition(XSort.ElementPosition);
            // pos.z = 0;
            XSort.transform.localPosition = pos;
            XSort.transform.localScale = Vector3.one;
            XSort.SetScale(LevelFitter.XSortSize);
            layers[XSort.ElementPosition] = XSort;
            m_ActXSorts.Add(XSort);

            return XSort;
        }



        public void SpawnObjects(XSortModel[] availableObjects)
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

            // 同步物理，确保后续遮挡检测读取到正确的 BoxCollider.bounds。
            Physics.SyncTransforms();
            IsBoardSpawned = true;
            CacheInitialXSortCount();
        }

        public void CacheInitialXSortCount()
        {
            m_initialXSortCount = m_ActXSorts.Count;
        }

        public void SetInitialXSortCount(int count)
        {
            m_initialXSortCount = count;
        }




        //收集level的空格
        private void CollectEmptyXSorts()
        {
            m_emptyXSorts.Clear();
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
                            XSortSpawnModel XSortSpawnData = new()
                            {
                                ElementPosition = new ElementPosition(x, y, i),  // 设置棋子位置
                                CellModel = cellData,                             // 设置单元格数据
                                LayerIndex = i,                                  // 设置层级索引
                                Layer = layer,                                   // 设置层级引用
                                LayerSize = size                                // 设置层级大小
                            };

                            m_emptyXSorts.Add(XSortSpawnData);
                        }
                    }
                }
            }
        }

        void ApplyCellEffect(XSortActor XSort, CellModel cellData)
        {
            if (cellData.Effect == XSortEffectType.None || cellData.Effect == XSortEffectType.GoldenTree)
            {
                return;
            }

            XSortEffect effect = LevelDirector.GetXSortEffect(cellData);
            if (effect != null)
            {
                effect.ApplyEffect(XSort);
            }
        }

        ///老的棋子分布规则
        private void SpawnObjectsOld(XSortModel[] availableObjects)
        {
            // 准备初始棋子类型分布（确保每种棋子类型都有合适的数量）
            XSortModel[] initialXSortsData = PrepareInitialXSorts(availableObjects);
            // 收集所有需要应用特效的棋子数据
            List<EffectModel> effectDatas = new();
            // 收集所有需要放置棋子的空位置信息
            List<XSortSpawnModel> emptyXSorts = new();
            CollectEmptyXSorts();
            emptyXSorts = m_emptyXSorts;

            if (!TrySpawnWithEasyHard34(initialXSortsData, emptyXSorts, effectDatas))
            {
                // eh3/4/5 失败时不要把 3/4/5 当 hard 打散权重
                int fallbackEh = m_dynamicData.easyHardValue == 1 ? 1 : 0;
                for (int i = 0; i < initialXSortsData.Length; i++)
                {
                    SpawnSetWithWeight(initialXSortsData[i], 3, emptyXSorts, fallbackEh, effectDatas);
                }
            }

            // 应用所有收集的特效
            foreach (EffectModel effectData in effectDatas)
            {
                if (effectData.EffectType == XSortEffectType.GoldenTree)
                    continue;

                XSortEffect effect = LevelDirector.GetXSortEffect(effectData.EffectType);
                if (effect != null)
                {
                    effect.ApplyEffect(effectData.XSortActor);
                }
            }
        }

        private void SpawnObjectsNew(XSortModel[] availableObjects)
        {
            // 准备初始棋子类型分布（确保每种棋子类型都有合适的数量）
            XSortModel[] initialXSortsData = PrepareInitialXSorts(availableObjects);//所有分配的棋子*3 

            m_XSortGenTypeDic.Clear(); //itemIdx对应的XSortModel
            m_XSortIndexHasDic.Clear(); //
            m_sameColorCntDic.Clear();
            m_XSortIndexCntDic.Clear();//每个itemIdx最终会生成多少棋子

            //初始化所有的XSort类型
            foreach (var item in initialXSortsData)
            {
                m_XSortGenTypeDic[item.XSortIndex] = item;

                if (m_XSortIndexCntDic.TryGetValue(item.XSortIndex, out int v)) //如果有+3,没有设置3
                {
                    m_XSortIndexCntDic[item.XSortIndex] += 3;
                }
                else
                {
                    m_XSortIndexCntDic[item.XSortIndex] = 3;
                }
            }

            m_emptyXSorts.Clear();
            CollectEmptyXSorts();

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
                    $"[LevelGen] eh={eh} 整盘拆组未生效，回退同色策略。sets={m_XSortIndexCntDic.Values.Sum() / 3}, empty={m_emptyXSorts.Count}");
            }

            string sameColorStr = m_dynamicData.sameColorStr ?? "";
            m_sameColorCntDic = ParseStrategyGenData(sameColorStr);

            int totalLayers = LevelDirector.Level.AmountOfLayers;
            for (int i = 0; i < totalLayers; i++)
            {
                GenOneLayerByStategy(i, ref initialXSortsData);
            }

            GenRestEmptyXSorts2();
        }

        /// <summary>
        /// 在同色策略之前，用当前全部待生成套数尝试 eh3/4/5 整盘放置。
        /// 成功则清空计数并返回 true；失败不改 empty/计数，返回 false。
        /// </summary>
        private bool TrySpawnEasyHardOnFullBoard()
        {
            var sets = new List<XSortModel>();
            var leftovers = new List<(XSortModel XSortData, int count)>();
            var typeTotalXSortCount = new Dictionary<int, int>();

            foreach (var item in m_XSortIndexCntDic)
            {
                if (item.Value <= 0)
                    continue;
                if (!m_XSortGenTypeDic.TryGetValue(item.Key, out XSortModel XSortData))
                    continue;

                typeTotalXSortCount[item.Key] = item.Value;
                int fullSets = item.Value / 3;
                int rem = item.Value % 3;
                for (int i = 0; i < fullSets; i++)
                    sets.Add(XSortData);
                if (rem > 0)
                    leftovers.Add((XSortData, rem));
            }

            if (!TrySpawnWithEasyHard34(sets.ToArray(), m_emptyXSorts, null, typeTotalXSortCount))
            {
                return false;
            }

            foreach (var leftover in leftovers)
                SpawnSetWithWeight(leftover.XSortData, leftover.count, m_emptyXSorts, 0, null);

            m_XSortIndexCntDic.Clear();
            return true;
        }

        //根据策略生成一层
        private void GenOneLayerByStategy(int layerIdx, ref XSortModel[] initialXSortsData)
        {
            m_XSortIndexHasDic.Clear();

            //如果有设置该层限定
            //本层所有空位
            var oneList = m_emptyXSorts.Where(XSort => XSort.LayerIndex == layerIdx).ToList();
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
                        int XSortIdx = GetRandomXSortIndex(sameColorNum); //做了去重处理
                        if (XSortIdx >= 0)
                        {
                            m_XSortIndexHasDic[XSortIdx] = true;
                            if (oneRandomList.Count > 0)
                            {
                                SpawnXSortToDstList(XSortIdx, ref oneRandomList, sameColorNum);
                            }
                            //放入对应位置
                        }
                    }
                }
            }

            Debug.Log("总共数量:是  " + m_XSortIndexHasDic.Keys.Count);
        }

        public void SpawnObjectsFromSave(RecoverLevelData rlData)
        {
            m_XSortGenTypeDic.Clear(); // itemIdx -> XSortModel
            m_XSortIndexHasDic.Clear();
            m_sameColorCntDic.Clear();
            m_XSortIndexCntDic.Clear();

            if (rlData != null)
            {
                var XSortsArr = LevelDirector.Database.XSorts;
                bool BindCoinFromList(List<RecoverLevelData.RLAXSortData> list)
                {
                    if (list == null)
                    {
                        return false;
                    }

                    foreach (var item in list)
                    {
                        if (!LevelDirector.IsCoinRewardSkin(item.XSortSkinName))
                        {
                            continue;
                        }

                        if (item.XSortIndex >= 0 && item.XSortIndex < XSortsArr.Length)
                        {
                            LevelDirector.SetCoinRewardXSortData(XSortsArr[item.XSortIndex]);
                            return true;
                        }
                    }

                    return false;
                }

                if (!BindCoinFromList(rlData.AllXSortDataList))
                {
                    BindCoinFromList(rlData.DockXSortIndices);
                }
            }

            // 逐个还原保存的棋子
            foreach (var item in rlData.AllXSortDataList)
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

                var XSortsArr = LevelDirector.Database.XSorts;
                XSortModel XSortData = XSortsArr[item.XSortIndex];

                // 放置棋子
                ElementPosition pos = new ElementPosition(item.x, item.y, item.layer);
                var spawnedXSort = SpawnXSort(XSortData, pos);
                if (LevelDirector.IsCoinRewardSkin(item.XSortSkinName)
                    || LevelDirector.IsCoinRewardXSort(XSortData))
                {
                    spawnedXSort.ApplyCoinRewardSkin();
                    if (LevelDirector.CoinRewardXSortData == null)
                    {
                        LevelDirector.SetCoinRewardXSortData(XSortData);
                    }
                }
                else
                {
                    spawnedXSort.UpdateRuntimeSkin(item.XSortSkinName);
                }

                if (item.isFission)
                    spawnedXSort.SetFission(true);

                if (item.effectType != XSortEffectType.None
                    && item.effectType != XSortEffectType.GoldenTree
                    && !string.IsNullOrEmpty(item.effectData)
                    && item.effectType != XSortEffectType.Link)
                {
                    XSortEffect effect = LevelDirector.GetXSortEffect(item.effectType);
                    if (effect != null)
                    {
                        effect.ApplyEffect(spawnedXSort);
                        spawnedXSort.Effect.SetEffectData(item.effectData);
                    }
                }
            }

            SetLoadSaveDynamicStr(rlData.dynamicStr);

            // 同步物理，确保后续遮挡检测读取到正确的 BoxCollider.bounds。
            Physics.SyncTransforms();
            IsBoardSpawned = true;

            if (rlData.initialXSortCount > 0)
                SetInitialXSortCount(rlData.initialXSortCount);
            else
                SetInitialXSortCount(level.GetAmountOfFilledCells());

            if (rlData.coinRewardTotalCount > 0)
            {
                LevelDirector.SetCoinRewardXSortTotalCount(rlData.coinRewardTotalCount);
            }
        }

        //生成
        private void SpawnXSortToDstList(int XSortIndex, ref List<XSortSpawnModel> dstList, int num)
        {
            if (m_XSortGenTypeDic.TryGetValue(XSortIndex, out XSortModel tData))
            {
                int idx = 0;

                for (int i = dstList.Count - 1; i >= 0; i--)
                {
                    var item = dstList[i];
                    if (idx < num)
                    {
                        var spawned = SpawnXSort(tData, item.ElementPosition);
                        if (spawned == null)
                        {
                            continue;
                        }

                        if (item.CellModel.Effect != XSortEffectType.None)
                        {
                            ApplyCellEffect(spawned, item.CellModel);
                            item.CellModel.CheckResetEffect();
                        }
                        MinusItemIndexCnt(XSortIndex, 1);
                        m_emptyXSorts.Remove(item);
                        dstList.RemoveAt(i);
                    }
                    idx++;
                }
            }
        }

        private void GenRestEmptyXSorts2()
        {
            var emptyXSorts = m_emptyXSorts;
            var sets = new List<XSortModel>();
            var leftovers = new List<(XSortModel XSortData, int count)>();
            // 记录拆分前各类型剩余子数，供 eh 判定「恰好3子」（避免 Value=4/5 被拆成 1套+零头后误判）
            var typeTotalXSortCount = new Dictionary<int, int>();

            foreach (var item in m_XSortIndexCntDic.ToList())
            {
                if (item.Value <= 0)
                    continue;

                if (!m_XSortGenTypeDic.TryGetValue(item.Key, out XSortModel XSortData))
                    continue;

                typeTotalXSortCount[item.Key] = item.Value;

                int fullSets = item.Value / 3;
                int rem = item.Value % 3;
                for (int i = 0; i < fullSets; i++)
                    sets.Add(XSortData);
                if (rem > 0)
                    leftovers.Add((XSortData, rem));

                m_XSortIndexCntDic[item.Key] = 0;
            }

            bool usedEasyHard34 = TrySpawnWithEasyHard34(sets.ToArray(), emptyXSorts, null, typeTotalXSortCount);
            // eh3/4/5 失败时不要把 3/4/5 当 hard 打散权重（RecalculateWeight 非0即 hard）
            int restEasyHard = usedEasyHard34 ? 0
                : (m_dynamicData.easyHardValue == 1 ? 1 : 0);

            if (!usedEasyHard34)
            {
                foreach (var set in sets)
                    SpawnSetWithWeight(set, 3, emptyXSorts, restEasyHard, null);
            }

            foreach (var leftover in leftovers)
                SpawnSetWithWeight(leftover.XSortData, leftover.count, emptyXSorts, restEasyHard, null);
        }

        /// <summary>
        /// easyHard=3：sets 奇数且&gt;5，从恰好3子的套中取5套拆组；
        /// 底2子优先铺满末层、不够再到倒数第二层；固定1子从「底子占用最深倒数层+2」起逐套+1。
        /// easyHard=4：sets 偶数且&gt;4，从恰好3子的套中取4套：
        /// 底子同样优先末层再倒数第二层；底放1则剩余2子固定到倒数4/5，底放2则剩余1子固定到倒数6/7。
        /// easyHard=5：同 eh4 底部分，固定层对调——底放2则剩余1子到倒数4/5，底放1则剩余2子到倒数6/7。
        /// 其余套按 easyHard=0 聚拢。条件不满足返回 false。
        /// </summary>
        /// <param name="typeTotalXSortCount">各类型剩余总子数；为 null 时用 sets 内出现次数×3 判断恰好3子</param>
        private bool TrySpawnWithEasyHard34(
            XSortModel[] sets,
            List<XSortSpawnModel> emptyXSorts,
            List<EffectModel> effectDatas,
            Dictionary<int, int> typeTotalXSortCount = null)
        {
            if (sets == null || sets.Length == 0 || emptyXSorts == null)
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
                typeSetCount.TryGetValue(s.XSortIndex, out int cnt);
                typeSetCount[s.XSortIndex] = cnt + 1;
            }

            bool IsExactThreeXSorts(XSortModel s)
            {
                if (s == null)
                    return false;
                if (typeTotalXSortCount != null)
                {
                    return typeTotalXSortCount.TryGetValue(s.XSortIndex, out int total) && total == 3;
                }
                return typeSetCount.TryGetValue(s.XSortIndex, out int c) && c == 1;
            }

            var exactThreeXSortSets = sets
                .Where(IsExactThreeXSorts)
                .GroupBy(s => s.XSortIndex)
                .Select(g => g.First())
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            if (exactThreeXSortSets.Count < specialNeed)
                return false;

            var special = exactThreeXSortSets.Take(specialNeed).ToList();
            var specialIndexes = new HashSet<int>(special.Select(s => s.XSortIndex));
            var rest = sets
                .Where(s => s != null && !specialIndexes.Contains(s.XSortIndex))
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            if (use3)
            {
                // eh3：5套均为底2子 + 1子固定层
                // 底子占用最深倒数层：末层空位>=10 → 1，否则最多占末两层 → 2
                // 上1子起始 = bottomDepth + 2（末层够10时从倒数第3层起），再逐套 +1
                int[] bottomCounts = { 2, 2, 2, 2, 2 };
                int fixedStartFromBottom = GetEh3FixedStartFromBottom(emptyXSorts, specialNeed * 2);

                for (int i = 0; i < special.Count; i++)
                {
                    SpawnSpecialSplitSet(special[i], bottomCounts[i], 1, fixedStartFromBottom + i, emptyXSorts, effectDatas);
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
                    SpawnSpecialSplitSet(special[i], plan.bottom, plan.fixedCnt, plan.fromBottom, emptyXSorts, effectDatas);
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
                    SpawnSpecialSplitSet(special[i], plan.bottom, plan.fixedCnt, plan.fromBottom, emptyXSorts, effectDatas);
                }
            }

            foreach (var set in rest)
            {
                SpawnSetWithWeight(set, 3, emptyXSorts, 0, effectDatas);
            }

            return true;
        }

        /// <summary>
        /// eh3 上1子起始倒数层 = 底子占用最深倒数层数 + 2。
        /// 底子规则：优先末层，不够再到倒数第二层；故末层空位够 bottomXSortCount 时深度为1（起始倒数第3），否则为2（起始倒数第4）。
        /// </summary>
        private int GetEh3FixedStartFromBottom(List<XSortSpawnModel> emptyXSorts, int bottomXSortCount)
        {
            int lastLayer = level.AmountOfLayers - 1;
            int lastAvail = emptyXSorts == null
                ? 0
                : emptyXSorts.Count(t => t.LayerIndex == lastLayer);
            int bottomDepth = lastAvail >= bottomXSortCount ? 1 : 2;
            return bottomDepth + 2;
        }

        /// <summary>
        /// 特殊套：底子优先末层、不够再到倒数第二层（可跨这两层）；
        /// fixedCount 子落到固定倒数层（整组同层，放不下整组下一层）；剩余 easy 聚拢。
        /// </summary>
        private void SpawnSpecialSplitSet(XSortModel XSortData, int bottomCount, int fixedCount, int fixedFromBottom, List<XSortSpawnModel> emptyXSorts, List<EffectModel> effectDatas)
        {
            int placed = 0;

            // 底子：优先铺满最后一层，不够再落到倒数第二层（不要求同组同层）
            var bottomXSorts = TakeEmptyXSortsPreferLastTwoLayers(emptyXSorts, bottomCount);
            foreach (var spawnData in bottomXSorts)
            {
                SpawnXSortAt(XSortData, spawnData, effectDatas);
                placed++;
            }

            // 剩余固定层：整组同层，目标层放不下则下一层
            var fixedXSorts = TakeEmptyXSortsOnFromBottomSameLayer(emptyXSorts, fixedFromBottom, fixedCount);
            foreach (var spawnData in fixedXSorts)
            {
                SpawnXSortAt(XSortData, spawnData, effectDatas);
                placed++;
            }

            int remaining = 3 - placed;
            if (remaining > 0)
                SpawnSetWithWeight(XSortData, remaining, emptyXSorts, 0, effectDatas);
        }

        /// <summary>
        /// 底子取位：优先从末层逐个取满，末层不够再到倒数第二层；不要求同组同层。
        /// </summary>
        private List<XSortSpawnModel> TakeEmptyXSortsPreferLastTwoLayers(List<XSortSpawnModel> emptyXSorts, int count)
        {
            var result = new List<XSortSpawnModel>();
            if (count <= 0 || emptyXSorts == null || emptyXSorts.Count == 0)
                return result;

            void TakeFromLayer(int layerIndex)
            {
                if (layerIndex < 0 || result.Count >= count)
                    return;

                var candidates = emptyXSorts.Where(t => t.LayerIndex == layerIndex)
                    .OrderBy(_ => UnityEngine.Random.value).ToList();
                foreach (var t in candidates)
                {
                    if (result.Count >= count)
                        break;
                    result.Add(t);
                    emptyXSorts.Remove(t);
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
        private List<XSortSpawnModel> TakeEmptyXSortsOnFromBottomSameLayer(List<XSortSpawnModel> emptyXSorts, int fromBottom, int count)
        {
            var result = new List<XSortSpawnModel>();
            if (count <= 0 || emptyXSorts == null || emptyXSorts.Count == 0)
                return result;

            for (int fb = fromBottom; fb <= level.AmountOfLayers; fb++)
            {
                int layerIndex = level.AmountOfLayers - fb;
                if (layerIndex < 0)
                    break;

                var candidates = emptyXSorts.Where(t => t.LayerIndex == layerIndex)
                    .OrderBy(_ => UnityEngine.Random.value).ToList();
                if (candidates.Count < count)
                    continue;

                for (int i = 0; i < count; i++)
                {
                    result.Add(candidates[i]);
                    emptyXSorts.Remove(candidates[i]);
                }
                return result;
            }

            return result;
        }

        private void SpawnXSortAt(XSortModel XSortData, XSortSpawnModel spawnData, List<EffectModel> effectDatas)
        {
            XSortActor behavior = SpawnXSort(XSortData, spawnData.ElementPosition);

            if (spawnData.CellModel.Effect == XSortEffectType.None)
                return;

            if (effectDatas != null)
            {
                effectDatas.Add(new EffectModel(behavior, spawnData.CellModel.Effect));
                spawnData.CellModel.CheckResetEffect();
            }
            else
            {
                ApplyCellEffect(behavior, spawnData.CellModel);
                spawnData.CellModel.CheckResetEffect();
            }
        }

        /// <summary>按权重放置同类型 XSortCount 枚棋子（先底层随机首子，再权重选其余）。</summary>
        private void SpawnSetWithWeight(XSortModel XSortData, int XSortCount, List<XSortSpawnModel> emptyXSorts, int easyHardValue, List<EffectModel> effectDatas)
        {
            if (XSortData == null || XSortCount <= 0 || emptyXSorts == null || emptyXSorts.Count == 0)
                return;

            XSortSpawnModel firstXSortSpawnData = emptyXSorts.OrderBy(x => UnityEngine.Random.value)
                .OrderBy(x => x.LayerIndex).FirstOrDefault();
            if (firstXSortSpawnData == null)
                return;

            emptyXSorts.Remove(firstXSortSpawnData);
            SpawnXSortAt(XSortData, firstXSortSpawnData, effectDatas);

            float totalWeight = 0;
            foreach (XSortSpawnModel emptyXSort in emptyXSorts)
            {
                emptyXSort.RecalculateWeight(firstXSortSpawnData.LayerIndex, easyHardValue);
                totalWeight += emptyXSort.RandomWeight;
            }

            for (int a = 0; a < XSortCount - 1; a++)
            {
                if (emptyXSorts.Count == 0)
                    break;

                XSortSpawnModel selectedXSortData = null;
                if (totalWeight > 0)
                {
                    float randomValue = UnityEngine.Random.Range(0, totalWeight);
                    float currentWeight = 0;
                    foreach (XSortSpawnModel emptyXSort in emptyXSorts)
                    {
                        currentWeight += emptyXSort.RandomWeight;
                        if (currentWeight >= randomValue)
                        {
                            selectedXSortData = emptyXSort;
                            break;
                        }
                    }
                }

                if (selectedXSortData == null)
                    selectedXSortData = emptyXSorts[UnityEngine.Random.Range(0, emptyXSorts.Count)];

                emptyXSorts.Remove(selectedXSortData);
                totalWeight -= selectedXSortData.RandomWeight;
                SpawnXSortAt(XSortData, selectedXSortData, effectDatas);
            }
        }

        private void MinusItemIndexCnt(int XSortIndex, int minusCnt)
        {
            if (m_XSortIndexCntDic.TryGetValue(XSortIndex, out int v))
            {
                m_XSortIndexCntDic[XSortIndex] -= minusCnt;
            }
        }


        //随机获得一个剩余数量大于cnt的XSortIndex
        private int GetRandomXSortIndex(int needNum)
        {
            foreach (var item in m_XSortIndexCntDic)
            {
                if (item.Value >= needNum && !m_XSortIndexHasDic.ContainsKey(item.Key))
                {
                    return item.Key;
                }
            }

            return -1;
        }

        public List<XSortActor> GetSortedXSorts()
        {
            List<XSortActor> sortedXSorts = new();
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
                            sortedXSorts.Add(layers[i][x, y]);
                        }
                    }
                }
            }

            return sortedXSorts;
        }

        public bool IsThereAnyXSortsOnLayer(int layerId)
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
        /// <param name="XSortPos">要检查的棋子位置</param>
        /// <returns>如果棋子可见返回true，否则返回false</returns>
        public bool IsXSortUnconcealed(XSortActor XSortBehavior)
        {
            var XSortPos = XSortBehavior.ElementPosition;
            // 最顶层（LayerId = 0）的棋子总是可见的
            if (XSortPos.LayerId == 0)
                return true;
            return IsXSortUnconcealedNew(XSortBehavior);
        }

        public bool IsXSortUnconcealedNew(XSortActor XSortBehavior)
        {
            var XSortPos = XSortBehavior.ElementPosition;

            // 最顶层（LayerId = 0）的棋子总是可见的
            if (XSortPos.LayerId == 0)
                return true;

            if (BoxCoverManager.IsXSortBlocked(XSortBehavior)
                || AwardColorEggCoverManager.IsXSortBlocked(XSortBehavior))
                return false;

            // 从当前层级向下检查所有底层，看是否有棋子遮挡了当前棋子 ,i小的再上方
            for (int i = XSortPos.LayerId - 1; i >= 0; i--)
            {
                var layerGrid = layers.GetLayerGrid(i);
                for (int x = 0; x < layerGrid.Width; x++)
                {
                    for (int y = 0; y < layerGrid.Height; y++)
                    {
                        var levelItem = layerGrid[x, y];
                        if (XSortStackUtil.IsCurrentUpperBlocker(
                                XSortBehavior, levelItem.XSort))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool IsXSortExists(ElementPosition elementPosition)
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
            for (int i = 0; i < m_ActXSorts.Count; i++)
            {
                m_ActXSorts[i].Clear();
            }

            m_ActXSorts.Clear();
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
            public XSortActor XSortActor;
            public XSortEffectType EffectType;

            public EffectModel(XSortActor XSortBehavior, XSortEffectType effectType)
            {
                XSortActor = XSortBehavior;
                EffectType = effectType;
            }
        }

        /// <summary>
        /// 棋子生成数据类，用于存储棋子生成位置的相关信息和权重
        /// </summary>
        private class XSortSpawnModel
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