
using System;
using FishFramework;
using FishTilePlay;
using Spine.Unity;
using TMPro;
using TileGamePlay;
using UnityEngine;
using UnityEngine.UI;

public class UIGoldTreeStart : UIGoldTreeStartBase
{
    public Action ClickAction;

    /// <summary>结算页打开时，closeBtn 仅开启活动，不执行 ClickAction。</summary>
    public bool CloseStartsActivityOnly;

    TextMeshProUGUI mTitleText;
    TextMeshProUGUI mOpenText;
    const string RankDogNodeName = "SkeletonGraphic (RankDog)";

    Transform mDogRoot;
    Transform mLeftDogSlot;
    Transform mMiddleDogSlot;
    Transform mRightDogSlot;
    SkeletonGraphic mLeftRankDog;
    SkeletonGraphic mMiddleRankDog;
    SkeletonGraphic mRightRankDog;
    GameObject mTopEff01;
    GameObject mTopEff02;
    GameObject mTopEff03;

    protected override void OnOpen()
    {
        base.OnOpen();
        CacheLeagueTextRefs();
        CacheDogDisplayRefs();
        RefreshLeagueTexts();
        RefreshDogTierDisplay();
    }

    void CacheLeagueTextRefs()
    {
        if (rectTransform == null)
            return;

        if (mTitleText == null)
        {
            var titleTran = rectTransform.Find("GoldTreeStar_Main/Tlite_Set/GoldTreeTitle01/Title_Text")
                ?? rectTransform.Find("GoldTreeStar_Main/GoldTreeTitle01/Title_Text");
            if (titleTran != null)
                mTitleText = titleTran.GetComponent<TextMeshProUGUI>();
        }

        if (mOpenText == null)
        {
            var openTran = rectTransform.Find("GoldTreeStar_Main/GoldTreeText_Base01/Text");
            if (openTran != null)
                mOpenText = openTran.GetComponent<TextMeshProUGUI>();
        }
    }

    void RefreshLeagueTexts()
    {
        LocalizedTMPRefresher.SetText(
            mTitleText,
            GoldTreeRobotRankRewardHelper.GetPlayerLeagueOpenTitle);

        LocalizedTMPRefresher.SetText(
            mOpenText,
            GoldTreeRobotRankRewardHelper.GetPlayerLeagueOpenText);
    }

    void CacheDogDisplayRefs()
    {
        if (rectTransform == null)
            return;

        if (mDogRoot == null)
        {
            mDogRoot = rectTransform.Find("GoldTreeStar_Main/Dog")
                ?? rectTransform.Find("Dog");
        }

        if (mDogRoot == null)
            return;

        CacheDogSlot(mDogRoot, "left", ref mLeftDogSlot, ref mLeftRankDog);
        CacheDogSlot(mDogRoot, "middle", ref mMiddleDogSlot, ref mMiddleRankDog);
        CacheDogSlot(mDogRoot, "right", ref mRightDogSlot, ref mRightRankDog);
        mTopEff01 ??= mDogRoot.Find("UI_TopEff01")?.gameObject;
        mTopEff02 ??= mDogRoot.Find("UI_TopEff02")?.gameObject;
        mTopEff03 ??= mDogRoot.Find("UI_TopEff03")?.gameObject;
    }

    static void CacheDogSlot(
        Transform dogRoot,
        string slotName,
        ref Transform slot,
        ref SkeletonGraphic rankDog)
    {
        slot ??= dogRoot.Find(slotName);
        if (slot == null)
            return;

        rankDog ??= slot.Find(RankDogNodeName)?.GetComponent<SkeletonGraphic>();
    }

    void RefreshDogTierDisplay()
    {
        CacheDogDisplayRefs();
        if (mDogRoot == null)
            return;

        int currentTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(
            GPUserDataManager.GPUserData.goldenTreeLeagueTier);
        var (leftTier, middleTier, rightTier, topEffIndex) = ResolveTierWindow(currentTier);

        ApplyDogSlot(mLeftDogSlot, mLeftRankDog, leftTier);
        ApplyDogSlot(mMiddleDogSlot, mMiddleRankDog, middleTier);
        ApplyDogSlot(mRightDogSlot, mRightRankDog, rightTier);
        RefreshTopEffects(topEffIndex);
    }

    static (int leftTier, int middleTier, int rightTier, int topEffIndex) ResolveTierWindow(int currentTier)
    {
        int minTier = GoldTreeRobotRankRewardHelper.MinTierId;
        int maxTier = GoldTreeRobotRankRewardHelper.MaxTierId;

        if (currentTier <= minTier)
            return (1, 2, 3, 1);

        if (currentTier >= maxTier)
            return (6, 7, 8, 3);

        return (currentTier - 1, currentTier, currentTier + 1, 2);
    }

    static void ApplyDogSlot(Transform slot, SkeletonGraphic rankDog, int tierId)
    {
        bool valid = GoldTreeRobotRankRewardHelper.IsValidTierId(tierId);
        slot?.gameObject.SetActiveEx(valid);
        if (!valid)
            return;

        ApplyRankDog(rankDog, tierId);
    }

    static void ApplyRankDog(SkeletonGraphic spine, int tierId)
    {
        if (spine == null)
            return;

        spine.gameObject.SetActiveEx(true);
        GoldTreeRankTierDogDisplay.ApplyRankDogSkeleton(spine, tierId);
        spine.AnimationState?.ClearTracks();
        spine.freeze = true;
        spine.Update(0f);
    }

    void RefreshTopEffects(int topEffIndex)
    {
        SetTopEffectActive(mTopEff01, topEffIndex == 1);
        SetTopEffectActive(mTopEff02, topEffIndex == 2);
        SetTopEffectActive(mTopEff03, topEffIndex == 3);
    }

    void SetTopEffectActive(GameObject effectRoot, bool active)
    {
        if (effectRoot == null)
            return;

        effectRoot.SetActiveEx(active);
        if (active)
            PlayParticles(effectRoot);
    }

    static void PlayParticles(GameObject effectRoot)
    {
        if (effectRoot == null)
            return;

        for (int i = 0; i < effectRoot.transform.childCount; i++)
            effectRoot.transform.GetChild(i).gameObject.SetActiveEx(true);

        var particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.gameObject.SetActiveEx(true);
            ps.Clear(true);
            ps.Play(true);
        }
    }

    protected override void OnClicked(Button button)
    {
        if (button == startBtn)
        {
            DismissWithActivityAndClickAction();
        }
        else if (button == closeBtn)
        {
            HandleCloseButtonClicked();
        }
    }

    protected override bool OnEscButtonPressed()
    {
        HandleCloseButtonClicked();
        return true;
    }

    void HandleCloseButtonClicked()
    {
        // 点 X：上报 activity status=9（排位仍会开启；与小狗「拒绝开启」语义不同）
        ReportRefuseOpenActivity();

        if (CloseStartsActivityOnly)
        {
            // 胜利页：X 与 Start 一样开启活动后回到上一级
            DismissWithActivityAndClickAction();
        }
        else
        {
            CloseSelf();
        }
    }

    static void ReportRefuseOpenActivity()
    {
        string uniqueId = GameData.GoldTreeActData != null
            ? GameData.GoldTreeActData.GetActivityUniqueId(true)
            : "";
        GameBAData.ExcuteActivity(9, uniqueId: uniqueId);
    }

    void DismissWithActivityAndClickAction()
    {
        var clickAction = ClickAction;
        CloseSelf(() =>
        {
            if (!GameData.GoldTreeActData.GetBNoInActivityTime())
            {
                ExcuteActivity();
            }

            clickAction?.Invoke();
        });
    }

    protected override void OnDestroyed()
    {
        ClickAction = null;
        CloseStartsActivityOnly = false;
        mDogRoot = null;
        mLeftDogSlot = null;
        mMiddleDogSlot = null;
        mRightDogSlot = null;
        mLeftRankDog = null;
        mMiddleRankDog = null;
        mRightRankDog = null;
        mTopEff01 = null;
        mTopEff02 = null;
        mTopEff03 = null;
        base.OnDestroyed();
    }


    void ExcuteActivity()
    {
        GameData.Rank.SaveCurrentBoard();//修改当前排行榜
        GameEvent.Send((int)EventNameDef.GoldTreeActOpened);
    }



}