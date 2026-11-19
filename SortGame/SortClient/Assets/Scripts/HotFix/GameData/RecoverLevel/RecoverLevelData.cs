
using System.Collections.Generic;
using FishXSortPlay;
using UnityEngine;

public class RecoverLevelData
{
    //来我们来分析下,所有的数据
    //每一层的棋子做出判定
    //进入关卡后,保存所有数据,每次移除变换数据.
    //ondestroy保存数据.
    [System.Serializable]
    public struct RLAXSortData
    {
        public int layer;
        public int x;
        public int y;
        public int XSortIndex; //LevelDataBase中的Index
        public string XSortSkinName; //XSortsNew资源名，旧存档为空时按XSortIndex恢复
        public int dockIndex;//恢复dock上的数据
        public XSortEffectType effectType; // 效果类型
        public string effectData; // 效果状态数据（JSON格式）
        public bool isFission;

    }

    [System.Serializable]
    public struct RLBoxCoverData
    {
        public BoxCoverResolvedPlacement placement;
        public bool opened;
        public bool collected;
        public int collectedItems;
    }

    public int LevelIndex;

    public string dynamicStr;

    public int costTime;

    public List<RLAXSortData> AllXSortDataList = new();
    public List<RLAXSortData> DockXSortIndices = new();
    public List<RLBoxCoverData> BoxCoverStates = new();
    public int boxCoverCollectedCount;
    public int boxCoverTotalCount;
    public int iqChallengeClearedCount;

    [System.Serializable]
    public struct RLAwardColorEggCoverData
    {
        public FishXSortPlay.AwardColorEggCoverPlacement placement;
        public bool collected;
    }

    public List<RLAwardColorEggCoverData> AwardColorEggCoverStates = new();
    public int awardColorEggCoverCollectedCount;

    /// <summary>玩家是否对棋盘做过操作（提交/消除/道具等）；仅初始布局快照时为 false。</summary>
    public bool hasUserProgress;

    /// <summary>进关时的棋子总数，用于自动完成剩余比例计算。</summary>
    public int initialXSortCount;

    /// <summary>金币关生成的金币棋子总数，恢复关卡后用于结算展示。</summary>
    public int coinRewardTotalCount;

    /// <summary>本局是否已激活 HighLight 置灰效果（中途退出再进需恢复）。</summary>
    public bool highLightActive;

    /// <summary>本局棋子底板。空名字表示用主题底板。有操作后续关时沿用，不重新抽。</summary>
    public bool XSortBaseStyleSet;
    public string XSortBaseStyleName;

    ///转换成json字符串
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    //将json字符串变成数据
    public static RecoverLevelData FromJson(string json)
    {
        return JsonUtility.FromJson<RecoverLevelData>(json);
    }

    private static string saveKey = "RecoverLevelData";
    private static RecoverLevelData lastSavedLayout;
    /// <summary>本局玩家是否做过操作。进局时清空，玩家操作后置 true。</summary>
    private static bool hasUserOperated;

    public static void ResetLayoutProgress()
    {
        hasUserOperated = false;
        lastSavedLayout = null;
    }

    /// <summary>显式标记玩家做过一次会改变棋盘的操作（提交棋子、消除、道具等）。</summary>
    public static void MarkUserProgress()
    {
        hasUserOperated = true;
    }

    public static bool HasLayoutProgress()
    {
        if (hasUserOperated) return true;
        var data = GetCurrentLevelData();
        return data != null && HasPersistedUserProgress(data);
    }

    public static void BeginRecoverSession(RecoverLevelData recoverData)
    {
        // 读档继续游戏：若存档本身已是玩家操作过的进度，保持进度态
        hasUserOperated = recoverData != null && HasPersistedUserProgress(recoverData);
        if (recoverData != null)
        {
            CacheLayoutSnapshot(recoverData);
        }
    }

    public static void SaveCurrentLevelData(bool ignoreGameActive = false)
    {
        if (!ignoreGameActive && !GameDirector.IsGameActive) return;

        var actXSorts = LevelDirector.LevelGenService.ActXSorts;
        var oneCls = new RecoverLevelData();

        oneCls.LevelIndex = LevelDirector.LoadedLevelIndex;

        oneCls.dynamicStr = LevelDirector.LevelGenService.GetDynamicCfgStr();

        foreach (var oneXSort in actXSorts)
        {
            RLAXSortData oneXSortData = new();
            oneXSortData.layer = oneXSort.ElementPosition.LayerId;
            oneXSortData.x = oneXSort.ElementPosition.X;
            oneXSortData.y = oneXSort.ElementPosition.Y;
            oneXSortData.XSortIndex = oneXSort.XSortModel.XSortIndex;
            oneXSortData.XSortSkinName = oneXSort.RuntimeSkinName;
            oneXSortData.isFission = oneXSort.IsFission;

            // 保存效果信息
            if (oneXSort.Effect != null)
            {
                oneXSortData.effectType = oneXSort.Effect.EffectType;
                oneXSortData.effectData = oneXSort.Effect.GetEffectData();
            }
            else
            {
                oneXSortData.effectType = XSortEffectType.None;
                oneXSortData.effectData = "";
            }

            oneCls.AllXSortDataList.Add(oneXSortData);
        }

        // 保存dock中的棋子
        oneCls.DockXSortIndices.Clear();
        var dock = LevelDirector.Dock;
        if (dock != null)
        {
            var dockIndices = dock.GetOccupiedXSortIndices();
            if (dockIndices != null && dockIndices.Count > 0)
            {
                foreach (var item in dockIndices) //这个只是存的XSortIndex
                {
                    RLAXSortData oneXSortData = new();
                    oneXSortData.x = item.ElementPosition.X;
                    oneXSortData.y = item.ElementPosition.Y;
                    oneXSortData.XSortIndex = item.XSortModel.XSortIndex;
                    oneXSortData.XSortSkinName = item.RuntimeSkinName;
                    oneXSortData.layer = item.ElementPosition.LayerId;
                    oneXSortData.isFission = item.IsFission;

                    oneCls.DockXSortIndices.Add(oneXSortData);
                }
            }
        }

        oneCls.costTime = GameBAData.GetCurrentCostTime();

        oneCls.initialXSortCount = LevelDirector.LevelGenService.InitialXSortCount;
        oneCls.coinRewardTotalCount = LevelDirector.CoinRewardXSortTotalCount;
        oneCls.highLightActive = PUHighLightBehavior.IsActive;
        oneCls.XSortBaseStyleSet = true;
        oneCls.XSortBaseStyleName = LevelDirector.LoadedXSortBaseStyle >= 0
            ? XSortBaseStyleTable.NameAt(LevelDirector.LoadedXSortBaseStyle)
            : "";
        BoxCoverManager.WriteRecoveryData(oneCls);
        AwardColorEggCoverManager.WriteRecoveryData(oneCls);
        IQChallengeManager.WriteRecoveryData(oneCls);

        oneCls.hasUserProgress = hasUserOperated;

        // 与上次写入完全一致则跳过，避免无意义的重复写盘
        if (lastSavedLayout != null
            && lastSavedLayout.hasUserProgress == oneCls.hasUserProgress
            && lastSavedLayout.highLightActive == oneCls.highLightActive
            && lastSavedLayout.XSortBaseStyleSet == oneCls.XSortBaseStyleSet
            && lastSavedLayout.XSortBaseStyleName == oneCls.XSortBaseStyleName
            && IsSameLayout(oneCls, lastSavedLayout))
        {
            return;
        }

        var str = oneCls.ToJson();
        PlayerPrefs.SetString(saveKey, str);
        CacheLayoutSnapshot(oneCls);
        // PlayerPrefs.Save();
    }

    public static void ClearCurrentLevelData()
    {
        PlayerPrefs.SetString(saveKey, "");
        lastSavedLayout = null;
        hasUserOperated = false;
        // PlayerPrefs.Save();
    }

    /// <summary>将内存中的存档落盘，用于回主城/退出等关键节点，防止重启后丢失。</summary>
    public static void Flush()
    {
        PlayerPrefs.Save();
    }

    private static bool HasPersistedUserProgress(RecoverLevelData data)
    {
        if (data == null) return false;
        if (data.hasUserProgress) return true;
        // 兼容旧存档：dock 有棋子，也视为玩家操作过
        if (data.DockXSortIndices != null && data.DockXSortIndices.Count > 0) return true;
        if (data.highLightActive) return true;
        return false;
    }

    public static bool ShouldRestoreBoxCoverLayout(RecoverLevelData data)
    {
        return HasPersistedUserProgress(data);
    }

    private static RecoverLevelData CloneLayout(RecoverLevelData source)
    {
        return new RecoverLevelData
        {
            LevelIndex = source.LevelIndex,
            dynamicStr = source.dynamicStr,
            costTime = source.costTime,
            hasUserProgress = source.hasUserProgress,
            initialXSortCount = source.initialXSortCount,
            coinRewardTotalCount = source.coinRewardTotalCount,
            highLightActive = source.highLightActive,
            XSortBaseStyleSet = source.XSortBaseStyleSet,
            XSortBaseStyleName = source.XSortBaseStyleName,
            boxCoverCollectedCount = source.boxCoverCollectedCount,
            boxCoverTotalCount = source.boxCoverTotalCount,
            iqChallengeClearedCount = source.iqChallengeClearedCount,
            awardColorEggCoverCollectedCount = source.awardColorEggCoverCollectedCount,
            AwardColorEggCoverStates = source.AwardColorEggCoverStates != null
                ? new List<RLAwardColorEggCoverData>(source.AwardColorEggCoverStates)
                : new List<RLAwardColorEggCoverData>(),
            AllXSortDataList = source.AllXSortDataList != null
                ? new List<RLAXSortData>(source.AllXSortDataList)
                : new List<RLAXSortData>(),
            DockXSortIndices = source.DockXSortIndices != null
                ? new List<RLAXSortData>(source.DockXSortIndices)
                : new List<RLAXSortData>(),
            BoxCoverStates = source.BoxCoverStates != null
                ? new List<RLBoxCoverData>(source.BoxCoverStates)
                : new List<RLBoxCoverData>(),
        };
    }

    private static void CacheLayoutSnapshot(RecoverLevelData source)
    {
        lastSavedLayout = CloneLayout(source);
    }

    private static bool IsSameLayout(RecoverLevelData current, RecoverLevelData other)
    {
        if (other == null) return false;
        if (current.LevelIndex != other.LevelIndex) return false;
        if (current.dynamicStr != other.dynamicStr) return false;
        if (current.boxCoverCollectedCount != other.boxCoverCollectedCount) return false;
        if (current.boxCoverTotalCount != other.boxCoverTotalCount) return false;
        if (current.iqChallengeClearedCount != other.iqChallengeClearedCount) return false;
        if (current.awardColorEggCoverCollectedCount != other.awardColorEggCoverCollectedCount) return false;
        if (!IsSameEggList(current.AwardColorEggCoverStates, other.AwardColorEggCoverStates)) return false;
        if (!IsSameXSortList(current.AllXSortDataList, other.AllXSortDataList)) return false;
        if (!IsSameXSortList(current.DockXSortIndices, other.DockXSortIndices)) return false;
        if (!IsSameBoxCoverList(current.BoxCoverStates, other.BoxCoverStates)) return false;
        return true;
    }

    private static bool IsSameEggList(List<RLAwardColorEggCoverData> a, List<RLAwardColorEggCoverData> b)
    {
        if (a == null || b == null) return a == b;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].placement.layer != b[i].placement.layer
                || a[i].placement.anchorX != b[i].placement.anchorX
                || a[i].placement.anchorY != b[i].placement.anchorY
                || a[i].collected != b[i].collected)
                return false;
        }
        return true;
    }

    private static bool IsSameBoxCoverList(List<RLBoxCoverData> a, List<RLBoxCoverData> b)
    {
        if (a == null || b == null) return a == b;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            RLBoxCoverData left = a[i];
            RLBoxCoverData right = b[i];
            if (left.placement.layer != right.placement.layer
                || left.placement.anchorX != right.placement.anchorX
                || left.placement.anchorY != right.placement.anchorY
                || left.placement.size != right.placement.size
                || left.opened != right.opened
                || left.collected != right.collected
                || left.collectedItems != right.collectedItems)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsSameXSortList(List<RLAXSortData> a, List<RLAXSortData> b)
    {
        if (a == null || b == null) return a == b;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (!IsSameXSort(a[i], b[i])) return false;
        }
        return true;
    }

    private static bool IsSameXSort(RLAXSortData a, RLAXSortData b)
    {
        return a.layer == b.layer
            && a.x == b.x
            && a.y == b.y
            && a.XSortIndex == b.XSortIndex
            && a.XSortSkinName == b.XSortSkinName
            && a.effectType == b.effectType
            && a.effectData == b.effectData
            && a.isFission == b.isFission;
    }

    public static RecoverLevelData GetCurrentLevelData()
    {
        var str = PlayerPrefs.GetString(saveKey, "");
        if (str.Length > 0)
        {
            var data = FromJson(str);
            if (data != null)
            {
                data.BoxCoverStates ??= new List<RLBoxCoverData>();
            }
            return data;
        }
        return null;
    }

    //是否合法 
    public bool IsValid()
    {
        int cnt = AllXSortDataList.Count + DockXSortIndices.Count;
        return AllXSortDataList.Count > 0 && cnt % 3 == 0;
    }
}
