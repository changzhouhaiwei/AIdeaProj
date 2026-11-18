using System;
using System.Collections.Generic;
using Core.Tools.MethodExtension;
using FishFramework;
using Spine.Unity;
using TileGameCore;
using TileGamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDailyRewardGet : UIDailyRewardGetBase
{
    private const string BoxOpenAnim = "Open";
    private const string BoxLoopAnim = "Loop";
    // ponytail: UIDailyRewardGet_Anim 时长。改片段长度时改这个常量。
    private const float OpenLockDuration = 1.7f;

    private readonly List<GameObject> mBgList = new();
    private readonly List<RectTransform> mCellList = new();
    private SkeletonGraphic mBoxSpine;
    private TextMeshProUGUI mWinStreakCountText;
    private Transform mUserFrameNodeRoot;
    private UserHead mUserHead;
    private Action mCloseAction;
    private Timer mOpenUnlockTimer;
    private bool mCanClose;

    public void SetCloseAction(Action action)
    {
        mCloseAction = action;
    }

    protected override void OnBindCompsAndEvents()
    {
        BindRefsSafely();
        if (okBtn != null)
        {
            BindEvent(okBtn);
        }
    }

    protected override void OnUnbindCompsAndEvents()
    {
        if (okBtn != null)
        {
            UnbindEvent(okBtn);
        }

        okBtn = null;
        cellBg1 = null;
        cellBg2 = null;
        cellBg3 = null;
        cellBg4 = null;
        line1 = null;
        line2 = null;
        cell1 = null;
        cell2 = null;
        cell3 = null;
        cell4 = null;
    }

    protected override void OnOpen()
    {
        if (!GameData.Activity.IsDailyVictoryActEnabled())
        {
            CloseWithCallback();
            return;
        }

        base.OnOpen();
        PlayBoxOpenAnim();
        ClaimRewardOnOpen();
        RefreshWinStreakCount();
        RefreshUserHeadNode();
        RefreshUserFrameNode();
        InitNodes();
        ShowTodayRewards();
    }

    protected override void PlayOpenAnim(Action onFinish = null)
    {
        mCanClose = false;
        CancelOpenUnlockTimer();
        base.PlayOpenAnim(onFinish);
        mOpenUnlockTimer = Delay(OpenLockDuration, () =>
        {
            mOpenUnlockTimer = null;
            mCanClose = true;
        });
    }

    private void CancelOpenUnlockTimer()
    {
        if (mOpenUnlockTimer == null)
        {
            return;
        }

        Unschedule(mOpenUnlockTimer);
        mOpenUnlockTimer = null;
    }

    private void RefreshWinStreakCount()
    {
        if (gameObject == null)
        {
            return;
        }

        AvatarFrameUiHelper.RefreshTotalWinCount(gameObject.transform, forceShowHonor: true);

        if (mWinStreakCountText == null)
        {
            mWinStreakCountText = FindDescendant<TextMeshProUGUI>(gameObject.transform, "winStreakCount");
        }

        if (mWinStreakCountText == null)
        {
            return;
        }

        LocalizedTMPRefresher.SetText(
            mWinStreakCountText,
            () => GameData.Activity.GetDailyVictoryTotalWinDays().ToString());
    }

    private void RefreshUserHeadNode()
    {
        EnsureUserHead();
        if (mUserHead == null || !GPUserDataManager.HasInit())
        {
            return;
        }

        int headId = GPUserDataManager.Instance.UserData.userhead;
        mUserHead.UpdateIcon(headId);
        mUserHead.UpdateFrame(ResolveDisplayFrameId());
        mUserHead.SetBSelected(false);
    }

    private void EnsureUserHead()
    {
        if (mUserHead != null)
        {
            return;
        }

        var headNodeTran = FindDescendant<Transform>(gameObject.transform, "UserHeadNode");
        var headBehaviour = headNodeTran != null ? headNodeTran.GetComponent<UIWidgetBehaviour>() : null;
        if (headBehaviour == null)
        {
            return;
        }

        mUserHead = CreateWidget<UserHead>("DailyRewardGetUserHead", headBehaviour);
    }

    /// <summary>领取后 userframe 已切到连胜框；否则用当前装备框。</summary>
    private static int ResolveDisplayFrameId()
    {
        if (!GPUserDataManager.HasInit() || GPUserDataManager.Instance.UserData == null)
        {
            return GPUserData.DefaultAvatarFrameId;
        }

        int frameId = GPUserDataManager.Instance.UserData.userframe;
        return frameId > 0 ? frameId : GPUserData.DefaultAvatarFrameId;
    }

    private void RefreshUserFrameNode()
    {
        if (mUserFrameNodeRoot == null && gameObject != null)
        {
            mUserFrameNodeRoot = FindDescendant<Transform>(gameObject.transform, "UserFrameNode");
        }

        if (mUserFrameNodeRoot == null)
        {
            return;
        }

        var iconTran = mUserFrameNodeRoot.Find("icon");
        if (iconTran != null)
        {
            iconTran.gameObject.SetActiveEx(false);
        }

        int frameId = ResolveDisplayFrameId();
        var cfg = ConfigHelper.Tables?.TbAvatarFrameCfg?.GetOrDefault(frameId);
        var frameTran = mUserFrameNodeRoot.Find("frame");
        if (frameTran != null)
        {
            var frameImg = frameTran.GetComponent<Image>();
            if (frameImg != null && cfg != null && !string.IsNullOrEmpty(cfg.Pic))
            {
                frameImg.sprite = LoadAtlasSprite("UserInfo", cfg.Pic);
            }

            frameTran.gameObject.SetActiveEx(cfg != null);
        }

        AvatarFrameUiHelper.ApplyFrameDecor(
            mUserFrameNodeRoot,
            frameId,
            unlocked: true,
            AvatarFrameDecorScene.RewardGetStatic);
        // 第 7 天领奖弹窗：强制打开 honor（展示连胜天数）
        AvatarFrameUiHelper.RefreshTotalWinCount(mUserFrameNodeRoot, frameId, forceShowHonor: true);
    }

    private void PlayBoxOpenAnim()
    {
        var boxSpine = EnsureBoxSpine();
        if (boxSpine == null)
        {
            return;
        }

        boxSpine.gameObject.SetActiveEx(true);
        if (!boxSpine.IsValid)
        {
            boxSpine.Initialize(false);
        }

        if (!boxSpine.IsValid)
        {
            return;
        }

        var state = boxSpine.AnimationState;
        state.ClearTrack(0);
        state.SetAnimation(0, BoxOpenAnim, false);
        state.AddAnimation(0, BoxLoopAnim, true, 0f);
        boxSpine.Update(0f);
    }

    private SkeletonGraphic EnsureBoxSpine()
    {
        if (mBoxSpine != null)
        {
            return mBoxSpine;
        }

        if (gameObject == null)
        {
            return null;
        }

        var boxRoot = FindDescendant<Transform>(gameObject.transform, "Box");
        if (boxRoot != null)
        {
            mBoxSpine = boxRoot.GetComponentInChildren<SkeletonGraphic>(true);
        }

        if (mBoxSpine == null)
        {
            mBoxSpine = FindDescendant<SkeletonGraphic>(gameObject.transform, "SkeletonGraphic (Box)");
        }

        return mBoxSpine;
    }

    private void ClaimRewardOnOpen()
    {
        // 本弹窗含 UserFrameNode，仅第 7 天头像框领取流程使用
        if (!GameData.Activity.CanClaimDailyVictoryAvatarFrame())
        {
            return;
        }

        GameData.Activity.ClaimCurrentStreakReward();
    }

    private void BindRefsSafely()
    {
        var root = gameObject != null ? gameObject.transform : null;
        if (root == null)
        {
            return;
        }

        okBtn = SafeGetBoundComponent<TButton>(0) ?? FindDescendant<TButton>(root, "okBtn");
        cellBg1 = SafeGetBoundComponent<Image>(1) ?? FindDescendant<Image>(root, "cellBg1");
        cellBg2 = SafeGetBoundComponent<Image>(2) ?? FindDescendant<Image>(root, "cellBg2");
        cellBg3 = SafeGetBoundComponent<Image>(3) ?? FindDescendant<Image>(root, "cellBg3");
        cellBg4 = SafeGetBoundComponent<Image>(4) ?? FindDescendant<Image>(root, "cellBg4");
        line1 = SafeGetBoundComponent<RectTransform>(5) ?? FindDescendant<RectTransform>(root, "line1");
        line2 = SafeGetBoundComponent<RectTransform>(6) ?? FindDescendant<RectTransform>(root, "line2");
        cell1 = SafeGetBoundComponent<RectTransform>(7) ?? FindDescendant<RectTransform>(root, "cell1");
        cell2 = SafeGetBoundComponent<RectTransform>(8) ?? FindDescendant<RectTransform>(root, "cell2");
        cell3 = SafeGetBoundComponent<RectTransform>(9) ?? FindDescendant<RectTransform>(root, "cell3");
        cell4 = SafeGetBoundComponent<RectTransform>(10) ?? FindDescendant<RectTransform>(root, "cell4");
    }

    private T SafeGetBoundComponent<T>(int elementIndex) where T : Component
    {
        if (viewBehaviour?.opElementList == null
            || elementIndex < 0
            || elementIndex >= viewBehaviour.opElementList.Count)
        {
            return null;
        }

        var componentList = viewBehaviour.opElementList[elementIndex].componentList;
        if (componentList == null || componentList.Count == 0)
        {
            return null;
        }

        return componentList[0] as T;
    }

    private void InitNodes()
    {
        mBgList.Clear();
        mCellList.Clear();
        TryAddBg(cellBg1);
        TryAddBg(cellBg2);
        TryAddBg(cellBg3);
        TryAddBg(cellBg4);
        TryAddCell(cell1);
        TryAddCell(cell2);
        TryAddCell(cell3);
        TryAddCell(cell4);
    }

    private void TryAddBg(Image bg)
    {
        if (bg != null)
        {
            mBgList.Add(bg.gameObject);
        }
    }

    private void TryAddCell(RectTransform cell)
    {
        if (cell != null)
        {
            mCellList.Add(cell);
        }
    }

    private void ShowTodayRewards()
    {
        var rewardCfg = GameData.Activity.GetCurrentStreakRewardCfg();
        var rewardList = GetDisplayRewardList(rewardCfg);
        if (mBgList.Count == 0 || mCellList.Count == 0)
        {
            return;
        }

        int slotCount = Mathf.Min(mBgList.Count, mCellList.Count);
        int count = Mathf.Min(rewardList.Count, slotCount);

        if (line2 != null)
        {
            line2.SetActiveEx(count >= 3);
        }

        float scale = count <= 1 ? 1.6f : 1.2f;
        for (int i = 0; i < mBgList.Count; i++)
        {
            bool shouldShow = i < count;
            mBgList[i].SetActive(shouldShow);
            if (shouldShow)
            {
                ShowRewardCell(mCellList[i].gameObject, rewardList[i]);
                mCellList[i].transform.SetLocalScale(scale);
            }
        }

        AudioController.PlaySound(AudioController.AudioClips.rewardGet);
    }

    /// <summary>
    /// cell 区只展示道具（共 4 个）；头像框由上方 UserFrameNode 展示。
    /// 按 IsAvatarFrame 过滤，避免领取后 tip 已不含头像框时误删第一个道具。
    /// </summary>
    private static List<ActivityData.DailyWinRewardTipData> GetDisplayRewardList(
        cfg.Game.DailyVictoryRewardCfg rewardCfg)
    {
        var fullList = GameData.Activity.BuildDailyWinRewardTipList(rewardCfg);
        if (fullList == null || fullList.Count == 0)
        {
            return fullList ?? new List<ActivityData.DailyWinRewardTipData>();
        }

        var result = new List<ActivityData.DailyWinRewardTipData>(fullList.Count);
        for (int i = 0; i < fullList.Count; i++)
        {
            var tip = fullList[i];
            if (tip != null && !tip.IsAvatarFrame)
            {
                result.Add(tip);
            }
        }

        return result;
    }

    private void ShowRewardCell(GameObject cell, ActivityData.DailyWinRewardTipData rewardData)
    {
        if (cell == null || rewardData == null)
        {
            return;
        }

        var text = cell.GetComponentInChildren<TextMeshProUGUI>();
        var icon = cell.GetComponentInChildren<Image>();
        if (text != null)
        {
            text.text = rewardData.CountText;
        }

        if (icon == null)
        {
            return;
        }

        Sprite rewardSprite = rewardData.UseAtlasSprite
            ? LoadAtlasSprite(rewardData.SpriteDirectory, rewardData.SpriteName)
            : LoadSprite(rewardData.SpriteDirectory, rewardData.SpriteName);

        if (rewardSprite == null)
        {
            rewardSprite = rewardData.UseAtlasSprite
                ? LoadSprite(rewardData.SpriteDirectory, rewardData.SpriteName)
                : LoadAtlasSprite(rewardData.SpriteDirectory, rewardData.SpriteName);
        }

        if (rewardSprite == null
            && !string.IsNullOrEmpty(rewardData.FallbackSpriteDirectory)
            && !string.IsNullOrEmpty(rewardData.FallbackSpriteName))
        {
            rewardSprite = LoadSprite(rewardData.FallbackSpriteDirectory, rewardData.FallbackSpriteName);
        }

        if (rewardSprite != null)
        {
            icon.sprite = rewardSprite;
        }
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button != okBtn || !mCanClose)
        {
            return;
        }

        AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        CloseWithCallback();
    }

    protected override bool OnEscButtonPressed()
    {
        if (!mCanClose)
        {
            return true;
        }

        CloseWithCallback();
        return true;
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        if (!mCanClose)
        {
            return;
        }

        if (bgClickEventType == UIPanelBgClickEventType.CloseSelf)
        {
            CloseWithCallback();
            return;
        }

        base.OnBackgroundClicked(bgClickEventType);
    }

    private void CloseWithCallback()
    {
        var action = mCloseAction;
        mCloseAction = null;
        CloseSelf(action);
    }

    protected override void OnDestroying()
    {
        if (mBoxSpine != null && mBoxSpine.IsValid)
        {
            mBoxSpine.AnimationState.ClearTrack(0);
            mBoxSpine = null;
        }

        CancelOpenUnlockTimer();
        mUserHead = null;
        mCloseAction = null;
        base.OnDestroying();
    }

    private static T FindDescendant<T>(Transform root, string nodeName) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == nodeName)
            {
                return child.GetComponent<T>();
            }
        }

        return null;
    }
}
