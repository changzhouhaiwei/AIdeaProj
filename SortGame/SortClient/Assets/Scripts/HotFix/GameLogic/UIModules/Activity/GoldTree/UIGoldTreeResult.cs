using System;
using System.Collections;
using System.Collections.Generic;
using Core.Tools.MethodExtension;
using TMPro;
using TileGamePlay;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIGoldTreeResult : UIGoldTreeResultBase
{
    const string ClaimAnimatorStateName = "UIGoldTreeResult_RankAwardCell_Move_Anim";

    List<GameObject> awardObjList = new();

    private cfg.Game.GoldTreeRankAwardCfg mAwardData;
    private long mSettlementRank = -1;
    private int mSettlementBoardId = -1;
    private int mCachedRankTier;
    private int mCachedGoldTileNum;
    private int mCachedLeaderboard;
    private int mCachedTotalLevel;
    private string mCachedUniqueId;
    private Transform mTop3Root;
    private Transform mNormalRoot;
    private TextMeshProUGUI mResultDescLb;
    private Coroutine mClaimAnimCoroutine;
    private Coroutine mRankRewardSoundCoroutine;
    private bool mClaimHandled;
    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);

        if (button == backBtn)
        {

        }
        else if (button == claimBtn)
        {
            PlayGetAward();
        }
    }

    void PlayGetAward()
    {
        if (mClaimHandled)
            return;

        claimBtn.SetActiveEx(false);

        var animator = gameObject_Animator;
        if (animator == null)
        {
            OnClaimAnimatorComplete();
            return;
        }

        StopClaimAnimCoroutine();
        animator.enabled = true;
        animator.Play(ClaimAnimatorStateName, 0, 0f);
        animator.Update(0f);
        mClaimAnimCoroutine = viewBehaviour.StartCoroutine(WaitClaimAnimatorComplete(animator));
    }

    IEnumerator WaitClaimAnimatorComplete(Animator animator)
    {
        yield return null;

        int stateHash = Animator.StringToHash(ClaimAnimatorStateName);
        float timeout = ResolveAnimatorClipDuration(animator, ClaimAnimatorStateName) + 0.15f;
        float elapsed = 0f;

        while (animator != null && gameObject != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == stateHash
                && stateInfo.normalizedTime >= 1f
                && !animator.IsInTransition(0))
            {
                break;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
                break;

            yield return null;
        }

        mClaimAnimCoroutine = null;
        OnClaimAnimatorComplete();
    }

    static float ResolveAnimatorClipDuration(Animator animator, string clipName)
    {
        var clips = animator?.runtimeAnimatorController?.animationClips;
        if (clips == null || clips.Length == 0)
            return 0f;

        foreach (var clip in clips)
        {
            if (clip != null && clip.name == clipName)
                return clip.length;
        }

        return clips[0] != null ? clips[0].length : 0f;
    }

    void OnClaimAnimatorComplete()
    {
        if (mClaimHandled)
            return;

        mClaimHandled = true;
        StopClaimAnimCoroutine();

        if (gameObject == null || !gameObject.activeInHierarchy)
            return;

        GameBAData.ExcuteRankClose(mCachedRankTier, mCachedGoldTileNum, mCachedLeaderboard, mCachedTotalLevel);
        GameBAData.ExcuteActivity(2, mCachedLeaderboard, mCachedRankTier, mCachedUniqueId, mCachedGoldTileNum);
        GameBAData.ExcuteGameTriggerActivity("1", "rank", 2, mCachedUniqueId);
        ApplyLeagueTierSettlementOnClaim();
        GPUserDataManager.Instance.UserData.goldenTreeActOpen = false;
        var rankView = UIRankView.Instance;
        if (rankView != null)
        {
            rankView.MarkSettlementClaimed();
            rankView.RefreshAfterLeagueTierChanged();
        }
        else if (GameData.Rank.HasPendingLastSeasonSettlement())
        {
            GameData.Rank.OnSettlementClaimedAndClosed();
        }

        GameEvent.Send((int)EventNameDef.GoldTreeRewardedFinish);
        CloseSelf(() =>
        {
            GameModule.UI.OpenPanel<UIGoldTreeLeagueEnd>();
        });
    }

    void StopClaimAnimCoroutine()
    {
        if (mClaimAnimCoroutine == null || viewBehaviour == null)
            return;

        viewBehaviour.StopCoroutine(mClaimAnimCoroutine);
        mClaimAnimCoroutine = null;
    }
    protected override void OnOpen()
    {
        base.OnOpen();
        mClaimHandled = false;
        mCachedRankTier = 0;
        mCachedGoldTileNum = 0;
        mCachedLeaderboard = 0;
        mCachedTotalLevel = 0;
        mCachedUniqueId = "";
        awardObjList.Clear();
        awardObjList.Add(rankAwardCell1.gameObject);
        awardObjList.Add(rankAwardCell2.gameObject);
        awardObjList.Add(rankAwardCell3.gameObject);
        awardObjList.Add(rankAwardCell4.gameObject);
        CacheRankDisplayNodes();
        ScheduleRankRewardSound();
    }

    void ScheduleRankRewardSound()
    {
        StopRankRewardSoundCoroutine();
        if (viewBehaviour == null)
            return;
        mRankRewardSoundCoroutine = viewBehaviour.StartCoroutine(PlayRankRewardSoundAfterDelay());
    }

    IEnumerator PlayRankRewardSoundAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);
        mRankRewardSoundCoroutine = null;
        var rewardClip = TileGameCore.AudioController.AudioClips?.rank_reward;
        if (rewardClip != null)
            TileGameCore.AudioController.PlaySound(rewardClip);
    }

    void StopRankRewardSoundCoroutine()
    {
        if (mRankRewardSoundCoroutine == null || viewBehaviour == null)
            return;
        viewBehaviour.StopCoroutine(mRankRewardSoundCoroutine);
        mRankRewardSoundCoroutine = null;
    }

    void CacheRankDisplayNodes()
    {
        if (rectTransform == null)
            return;

        var imageRoot = rectTransform.Find("nineBg/Image_Root");
        if (imageRoot == null)
            return;

        mTop3Root = imageRoot.Find("1-3");
        mNormalRoot = imageRoot.Find("normal");

        var descTran = rectTransform.Find("nineBg/Text (TMP)");
        mResultDescLb = descTran != null ? descTran.GetComponent<TextMeshProUGUI>() : null;
    }

    public void ShowRank(long rank, RankData.OneUserData userData = null)
    {
        mSettlementRank = rank;
        CacheRankClosePayload(rank);
        if (mNormalRoot == null)
            CacheRankDisplayNodes();

        var myData = userData ?? GameData.Rank.GetMyRankData();

        RefreshResultDescText(rank);

        mTop3Root?.SetActiveEx(false);
        mNormalRoot.SetActiveEx(true);
        ShowNormalRank(myData, rank);
    }

    void CacheRankClosePayload(long rank)
    {
        var userData = GPUserDataManager.GPUserData;
        mCachedRankTier = GoldTreeLeagueRankFlagHelper.ResolveSettlementBaseTier(ResolveSettlementBoardId());
        mCachedGoldTileNum = userData != null ? userData.goldenTreeActPoint : 0;
        mCachedLeaderboard = (int)rank;
        mCachedTotalLevel = GPUserDataManager.Instance.GetPassLevelCnt();
        mCachedUniqueId = GameData.GoldTreeActData != null ? GameData.GoldTreeActData.GetActivityUniqueId() : "";
    }

    void RefreshResultDescText(long rank)
    {
        if (mResultDescLb == null)
            return;

        LocalizedTMPRefresher.SetText(mResultDescLb, () =>
            string.Format(I2LocalizeMgr.GetString("UI_RankingEvent_RankEndRewards"), rank));
    }

    void ShowNormalRank(RankData.OneUserData data, long rank)
    {
        if (rankLb != null)
            rankLb.text = "#" + rank;

        if (mNormalRoot == null)
            return;

        SetAllChildrenActive(mNormalRoot, true);

        if (data == null)
            return;

        var nameLb = mNormalRoot.Find("nameLb")?.GetComponent<TextMeshProUGUI>();
        var scoreLb = FindNormalChildText("scoreLb");
        var headIcon = FindNormalChildImage("headIcon");

        if (nameLb != null)
            nameLb.text = data.playerName;
        if (scoreLb != null)
            scoreLb.text = data.score.ToString();
        if (headIcon != null)
            headIcon.sprite = LoadAtlasSprite("UserInfo", "Avatar_" + data.pictureUrl);
    }

    TextMeshProUGUI FindNormalChildText(string nodeName)
    {
        return FindChildComponentRecursive<TextMeshProUGUI>(mNormalRoot, nodeName);
    }

    Image FindNormalChildImage(string nodeName)
    {
        return FindChildComponentRecursive<Image>(mNormalRoot, nodeName);
    }

    static Transform FindChildRecursive(Transform parent, string nodeName)
    {
        if (parent == null)
            return null;

        if (IsSameNodeName(parent.name, nodeName))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindChildRecursive(parent.GetChild(i), nodeName);
            if (found != null)
                return found;
        }

        return null;
    }

    static T FindChildComponentRecursive<T>(Transform parent, string nodeName) where T : Component
    {
        var node = FindChildRecursive(parent, nodeName);
        return node != null ? node.GetComponent<T>() : null;
    }

    static bool IsSameNodeName(string actualName, string expectedName)
    {
        return string.Equals(actualName, expectedName, StringComparison.Ordinal)
            || string.Equals(actualName.Trim(), expectedName, StringComparison.Ordinal);
    }

    static void SetAllChildrenActive(Transform parent, bool active)
    {
        if (parent == null)
            return;

        for (int i = 0; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActiveEx(active);
    }

    void ApplyLeagueTierSettlementOnClaim()
    {
        if (mSettlementRank < 1)
            return;

        GameData.Rank.ApplyTierSettlementOnClaim((int)mSettlementRank, mSettlementBoardId);
    }

    //显示奖励
    public void ShowAwards(cfg.Game.GoldTreeRankAwardCfg awardCfg)
    {
        mAwardData = awardCfg;

        if (awardCfg == null)
        {
            GameData.GoldTreeActData.ClearAllActivityData();
            Delay(0.1f, () =>
            {
                CloseSelf(() => GameData.GoldTreeActData?.NotifySettlementFlowClosed());
            });
            return;
        }

        int settlementBoardId = ResolveSettlementBoardId();
        mSettlementBoardId = settlementBoardId;
        bool alreadyGranted = settlementBoardId >= 0
            && GPUserDataManager.GPUserData.goldenTreeSettlementRewardGrantedBoardId == settlementBoardId;

        if (!alreadyGranted)
        {
            foreach (var item in awardCfg.Award)
            {
                var tempList = item.Split(",");
                var id = tempList[0].ToInt();
                var num = tempList[1].ToInt();
                GPUserDataManager.Instance.AddCommonItemData(id, num);
            }

            if (settlementBoardId >= 0)
            {
                GPUserDataManager.GPUserData.goldenTreeSettlementRewardGrantedBoardId = settlementBoardId;
                GPUserDataManager.Instance.SaveData();
            }
        }

        GameData.GoldTreeActData.ClearAllActivityData();

        TileGameCore.Tween.DelayedCall(0.1f, () =>
        {
            for (int i = 0; i < 4; i++)
            {
                var cellObj = awardObjList[i];
                cellObj.SetActive(i < awardCfg.Award.Count);
                if (i < awardCfg.Award.Count)
                {
                    var oneData = awardCfg.Award[i];
                    var tempList = oneData.Split(",");
                    ShowIconsAndNum(cellObj, tempList[0], tempList[1]);
                }
            }
        });
    }


    static int ResolveSettlementBoardId()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData.goldenTreeLastSeasonPendingReward && userData.goldenTreeLastSeasonBoardId >= 0)
        {
            return userData.goldenTreeLastSeasonBoardId;
        }

        if (userData.goldenTreeBoardId >= 0)
        {
            return userData.goldenTreeBoardId;
        }

        return -1;
    }

    private void ShowIconsAndNum(GameObject cell, string id, string num)
    {

        TextMeshProUGUI text = cell.GetComponentInChildren<TextMeshProUGUI>();
        Image icon = cell.GetComponentInChildren<Image>();

        text.text = "x" + num;
        icon.sprite = LoadSprite("LevelAward", "item_" + id);
    }

    protected override void OnDestroyed()
    {
        StopClaimAnimCoroutine();
        StopRankRewardSoundCoroutine();
        base.OnDestroyed();
    }
}