using System;
using System.Collections.Generic;
using DG.Tweening;
using FishFramework;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DGSequence = DG.Tweening.Sequence;

/// <summary>
/// UIRankView / UIGoldTreeLeagueEnd dogSwitch：5 个固定物理节点按车道轮播，段位切换 RankDog1~8 Spine。
/// </summary>
public class GoldTreeRankTierDogDisplay
{
    public const float DefaultShiftDuration = 0.45f;

    const float SideScale = 0.6f;
    const float CenterScale = 0.8f;
    const int InvalidTier = -1;
    const string PassIdleAnim = "pass_idle";
    const string PassLockShakeAnim = "pass_lock_shake";
    const string RankDogNodeName = "SkeletonGraphic (RankDog)";
    const string RankDogInAnim = "RankDog_In";
    const string RankDogIdleAnim = "RankDog_Idle";
    const string RankDogSkeletonDataPathFormat =
        "Assets/Arts/Art/Spine/RankDog/RankDog{0}_SkeletonData.asset";
    const string PedestalImageName = "Image";
    const string PedestalImageLeagueEndName = "Image (1)";
    static readonly Color LockedDogColor = new Color32(0x87, 0x87, 0x87, 0xFF);
    static readonly Color UnlockedDogColor = Color.white;

    enum CarouselLane
    {
        OffLeft = 0,
        Left = 1,
        Middle = 2,
        Right = 3,
        OffRight = 4,
    }

    struct SlotLayout
    {
        public Vector2 OffLeftPos;
        public Vector2 LeftPos;
        public Vector2 MiddlePos;
        public Vector2 RightPos;
        public Vector2 OffRightPos;
    }

    struct CarouselSlot
    {
        public GameObject Node;
        public SkeletonGraphic RankDog;
        public RectTransform Rt;
        public int TierId;
        public CarouselLane Lane;
    }

    struct LockClickBinding
    {
        public Button Button;
        public UnityAction Handler;
    }

    static readonly string[] PhysicalNodeNames =
    {
        "left (1)",
        "left",
        "middle",
        "right",
        "right (1)",
    };

    SlotLayout mLayout;
    CarouselSlot[] mSlots;
    Transform mDogRoot;
    Button mBtnLeft;
    Button mBtnRight;
    Action<int> mOnViewTierChanged;
    int mViewTierId;
    bool mIsAnimating;
    bool mShiftCompleted;
    bool mSuppressLockState;
    int mHideLockSpineBelowTier = InvalidTier;
    int mLockReferenceTier = InvalidTier;
    Tween mShiftTween;
    int mEnterSlotIndex = -1;
    float mShiftDuration = DefaultShiftDuration;
    Action mOnSettlementComplete;
    bool mEnableInteractiveLockSpine;
    readonly List<LockClickBinding> mLockClickBindings = new();
    TrackEntry mActiveShakeTrackEntry;
    Spine.AnimationState.TrackEntryDelegate mActiveShakeCompleteHandler;

    public int ViewTierId => mViewTierId;
    public bool IsAnimating => mIsAnimating;

    public static string GetDogSpriteName(int tierId) => $"dog/dog_{tierId}";

    public static string GetRankDogSkeletonDataPath(int tierId) =>
        string.Format(RankDogSkeletonDataPathFormat, tierId);

    public void Bind(
        Transform root,
        Button btnLeft = null,
        Button btnRight = null,
        Action<int> onViewTierChanged = null,
        float shiftDuration = DefaultShiftDuration,
        bool enableInteractiveLockSpine = false,
        bool useLeagueEndPedestal = false)
    {
        mShiftDuration = Mathf.Max(0.05f, shiftDuration);
        mBtnLeft = btnLeft;
        mBtnRight = btnRight;
        mOnViewTierChanged = onViewTierChanged;
        mEnableInteractiveLockSpine = enableInteractiveLockSpine;

        var dogRoot = ResolveDogRoot(root);
        if (dogRoot == null)
            return;

        mDogRoot = dogRoot;
        CacheLayout(dogRoot);
        BindPhysicalSlots(dogRoot);
        ApplyPedestalImages(useLeagueEndPedestal);
        if (mEnableInteractiveLockSpine)
            BindLockClicks();
    }

    static Transform ResolveDogRoot(Transform root)
    {
        if (root == null)
            return null;

        var dogRoot = root.Find("dog") ?? root.Find("dogSwitch");
        if (dogRoot != null)
            return dogRoot;

        return root.Find("middle") != null ? root : null;
    }

    void ApplyPedestalImages(bool useLeagueEndPedestal)
    {
        ApplyPedestalOnNode(mDogRoot, useLeagueEndPedestal);
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            if (mSlots[i].Node != null)
                ApplyPedestalOnNode(mSlots[i].Node.transform, useLeagueEndPedestal);
        }
    }

    static void ApplyPedestalOnNode(Transform node, bool useLeagueEndPedestal)
    {
        if (node == null)
            return;

        var image = node.Find(PedestalImageName);
        var leagueEndImage = node.Find(PedestalImageLeagueEndName);
        image?.gameObject.SetActiveEx(!useLeagueEndPedestal);
        leagueEndImage?.gameObject.SetActiveEx(useLeagueEndPedestal);
    }

    void BindPhysicalSlots(Transform dogRoot)
    {
        mSlots = new CarouselSlot[PhysicalNodeNames.Length];
        for (int i = 0; i < PhysicalNodeNames.Length; i++)
        {
            var node = dogRoot.Find(PhysicalNodeNames[i]);
            mSlots[i] = new CarouselSlot
            {
                Node = node != null ? node.gameObject : null,
                RankDog = GetRankDogSpine(node),
                Rt = node as RectTransform,
                TierId = InvalidTier,
                Lane = (CarouselLane)i,
            };
        }
    }

    void CacheLayout(Transform dogRoot)
    {
        var offLeftRt = dogRoot.Find("left (1)") as RectTransform;
        var leftRt = dogRoot.Find("left") as RectTransform;
        var middleRt = dogRoot.Find("middle") as RectTransform;
        var rightRt = dogRoot.Find("right") as RectTransform;
        var offRightRt = dogRoot.Find("right (1)") as RectTransform;

        if (leftRt == null || middleRt == null || rightRt == null)
            return;

        mLayout = new SlotLayout
        {
            OffLeftPos = offLeftRt != null ? offLeftRt.anchoredPosition : new Vector2(-476f, 89f),
            LeftPos = leftRt.anchoredPosition,
            MiddlePos = middleRt.anchoredPosition,
            RightPos = rightRt.anchoredPosition,
            OffRightPos = offRightRt != null ? offRightRt.anchoredPosition : new Vector2(471f, 89f),
        };

        NormalizeOffScreenPositions();
    }

    void NormalizeOffScreenPositions()
    {
        float step = Mathf.Abs(mLayout.RightPos.x - mLayout.MiddlePos.x);
        if (step < 1f)
            step = Mathf.Abs(mLayout.MiddlePos.x - mLayout.LeftPos.x);
        if (step < 1f)
            step = 222f;

        if (Mathf.Abs(mLayout.OffLeftPos.x - mLayout.LeftPos.x) < step * 0.5f)
            mLayout.OffLeftPos = new Vector2(mLayout.LeftPos.x - step, mLayout.LeftPos.y);

        if (Mathf.Abs(mLayout.OffRightPos.x - mLayout.RightPos.x) < step * 0.5f)
            mLayout.OffRightPos = new Vector2(mLayout.RightPos.x + step, mLayout.RightPos.y);
    }

    public void Dispose()
    {
        mOnSettlementComplete = null;
        KillShiftTween();
        ClearActiveShakeCallback();
        UnbindLockClicks();
        mSuppressLockState = false;
        mHideLockSpineBelowTier = InvalidTier;
        mLockReferenceTier = InvalidTier;
        mEnableInteractiveLockSpine = false;
        mDogRoot = null;
        mBtnLeft = null;
        mBtnRight = null;
        mOnViewTierChanged = null;
        mSlots = null;
    }

    public void ResetToPlayerTier()
    {
        ResetToViewTier(GPUserDataManager.GPUserData.goldenTreeLeagueTier);
    }

    public void ResetToViewTier(int viewTierId)
    {
        KillShiftTween();
        mSuppressLockState = false;
        mHideLockSpineBelowTier = InvalidTier;
        mLockReferenceTier = InvalidTier;
        mViewTierId = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(viewTierId);
        LayoutCarouselAtRest();
        UpdateButtonState();
        NotifyViewTierChanged();
    }

    /// <summary>
    /// 结算榜静态展示：middle 为结算后段位，锁状态以 newTier 为参考（middle/left 解锁，right 锁定）。
    /// </summary>
    public void ResetToSettlementViewTier(int newTier)
    {
        KillShiftTween();
        mSuppressLockState = false;
        mHideLockSpineBelowTier = InvalidTier;
        mLockReferenceTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(newTier);
        mViewTierId = mLockReferenceTier;
        LayoutCarouselAtRest();
        UpdateButtonState();
        NotifyViewTierChanged();
    }

    /// <summary>
    /// 联赛结算页：以 oldTier 为初始 middle，按 newTier 与 oldTier 差值播放升/保/降段动画。
    /// </summary>
    public void PlaySettlementTransition(
        int oldTier,
        int newTier,
        bool suppressLockState = true,
        Action onComplete = null)
    {
        mOnSettlementComplete = null;
        KillShiftTween();
        mOnSettlementComplete = onComplete;
        mSuppressLockState = suppressLockState;
        mViewTierId = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(oldTier);
        newTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(newTier);
        mHideLockSpineBelowTier = newTier;

        if (mSlots == null || mSlots.Length == 0)
        {
            mViewTierId = newTier;
            NotifyViewTierChanged();
            InvokeSettlementComplete();
            return;
        }

        int delta = newTier - mViewTierId;
        LayoutCarouselAtRest(playMiddleEnter: delta == 0);
        if (delta == 0)
        {
            UpdateButtonState();
            NotifyViewTierChanged();
            InvokeSettlementComplete();
            return;
        }

        PlayShiftAnimation(delta, newTier);
    }

    public bool TryShiftView(int delta)
    {
        if (delta == 0 || mIsAnimating)
            return false;

        int minTier = GoldTreeRobotRankRewardHelper.MinTierId;
        int maxTier = GoldTreeRobotRankRewardHelper.MaxTierId;
        int nextTier = Mathf.Clamp(mViewTierId + delta, minTier, maxTier);
        if (nextTier == mViewTierId)
            return false;

        if (mSlots == null || mSlots.Length == 0)
        {
            mViewTierId = nextTier;
            NotifyViewTierChanged();
            return true;
        }

        PlayShiftAnimation(delta, nextTier);
        return true;
    }

    public void Refresh()
    {
        KillShiftTween();
        LayoutCarouselAtRest();
        UpdateButtonState();
        NotifyViewTierChanged();
    }

    void NotifyViewTierChanged()
    {
        mOnViewTierChanged?.Invoke(mViewTierId);
    }

    void LayoutCarouselAtRest(bool playMiddleEnter = true)
    {
        RestoreDefaultLanes();
        ApplyTiersForView(mViewTierId);
        SnapAllSlotsToLanes();
        ApplySlotDrawOrder(CarouselLane.Middle);
        ApplyRankDogAnimsAtRest(playMiddleEnter);
    }

    void RestoreDefaultLanes()
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
            mSlots[i].Lane = (CarouselLane)i;
    }

    void ApplyTiersForView(int viewTier)
    {
        if (mSlots == null)
            return;

        SetSlotContent(ref mSlots[0], InvalidTier, hide: true);
        SetSlotContent(ref mSlots[1], viewTier - 1, hide: false);
        SetSlotContent(ref mSlots[2], viewTier, hide: false);
        SetSlotContent(ref mSlots[3], viewTier + 1, hide: false);
        SetSlotContent(ref mSlots[4], InvalidTier, hide: true);
    }

    void SetSlotContent(ref CarouselSlot slot, int tierId, bool hide)
    {
        if (slot.Node == null)
            return;

        slot.TierId = tierId;
        bool visible = !hide && GoldTreeRobotRankRewardHelper.IsValidTierId(tierId);
        if (!visible && hide)
        {
            slot.Node.SetActiveEx(false);
            return;
        }

        if (!visible)
        {
            slot.Node.SetActiveEx(false);
            return;
        }

        slot.Node.SetActiveEx(true);
        ApplyRankDogSkeleton(slot.RankDog, tierId);
        ApplyLockState(slot.Node, tierId);
    }

    void PlayShiftAnimation(int delta, int nextTier)
    {
        KillShiftTween();
        mIsAnimating = true;
        mShiftCompleted = false;
        SetButtonsInteractable(false);
        PlayRankDogAnimsForShift();

        int enteringTier = delta > 0 ? mViewTierId + 2 : mViewTierId - 2;
        bool hasEntering = GoldTreeRobotRankRewardHelper.IsValidTierId(enteringTier);
        mEnterSlotIndex = -1;

        if (hasEntering)
        {
            var dormant = AcquireOffScreenEnterSlot(delta > 0 ? CarouselLane.OffRight : CarouselLane.OffLeft);
            if (dormant.HasValue)
                mEnterSlotIndex = FindSlotIndex(dormant.Value.Node);
        }

        var seq = DOTween.Sequence();

        if (delta > 0)
            BuildShiftRightSequence(seq, enteringTier, hasEntering);
        else
            BuildShiftLeftSequence(seq, enteringTier, hasEntering);

        RefreshVisibleLocks();
        ApplySlotDrawOrder(delta > 0 ? CarouselLane.Right : CarouselLane.Left);

        if (mDogRoot != null)
            seq.SetLink(mDogRoot.gameObject, LinkBehaviour.KillOnDestroy);

        seq.OnComplete(() => CompleteShift(nextTier, delta, hasEntering));
        seq.OnKill(() =>
        {
            if (!mShiftCompleted)
                CompleteShift(nextTier, delta, hasEntering);
        });

        mShiftTween = seq;
        seq.Play();
    }

    void BuildShiftRightSequence(DGSequence seq, int enteringTier, bool hasEntering)
    {
        if (hasEntering && mEnterSlotIndex >= 0)
        {
            var slot = mSlots[mEnterSlotIndex];
            PrepareEnterSlot(ref slot, enteringTier, CarouselLane.OffRight);
            mSlots[mEnterSlotIndex] = slot;
            seq.Join(TweenSlotToLane(slot, CarouselLane.Right));
        }

        for (int i = 0; i < mSlots.Length; i++)
        {
            if (i == mEnterSlotIndex)
                continue;

            var slot = mSlots[i];
            if (slot.Node == null || !slot.Node.activeSelf)
                continue;

            switch (slot.Lane)
            {
                case CarouselLane.Left:
                    seq.Join(TweenSlotToLane(slot, CarouselLane.OffLeft));
                    break;
                case CarouselLane.Middle:
                    seq.Join(TweenSlotToLane(slot, CarouselLane.Left));
                    break;
                case CarouselLane.Right:
                    seq.Join(TweenSlotToLane(slot, CarouselLane.Middle));
                    break;
            }
        }
    }

    void BuildShiftLeftSequence(DGSequence seq, int enteringTier, bool hasEntering)
    {
        if (hasEntering && mEnterSlotIndex >= 0)
        {
            var slot = mSlots[mEnterSlotIndex];
            PrepareEnterSlot(ref slot, enteringTier, CarouselLane.OffLeft);
            mSlots[mEnterSlotIndex] = slot;
            seq.Join(TweenSlotToLane(slot, CarouselLane.Left));
        }

        for (int i = 0; i < mSlots.Length; i++)
        {
            if (i == mEnterSlotIndex)
                continue;

            var slot = mSlots[i];
            if (slot.Node == null || !slot.Node.activeSelf)
                continue;

            switch (slot.Lane)
            {
                case CarouselLane.Right:
                    seq.Join(TweenSlotToLane(slot, CarouselLane.OffRight));
                    break;
                case CarouselLane.Middle:
                    seq.Join(TweenSlotToLane(slot, CarouselLane.Right));
                    break;
                case CarouselLane.Left:
                    seq.Join(TweenSlotToLane(slot, CarouselLane.Middle));
                    break;
            }
        }
    }

    int FindSlotIndex(GameObject node)
    {
        for (int i = 0; i < mSlots.Length; i++)
        {
            if (mSlots[i].Node == node)
                return i;
        }

        return -1;
    }

    CarouselSlot? AcquireOffScreenEnterSlot(CarouselLane startLane)
    {
        int dedicatedIndex = startLane == CarouselLane.OffRight ? 4 : 0;
        if (IsSlotAvailableForEnter(mSlots[dedicatedIndex]))
            return mSlots[dedicatedIndex];

        return FindDormantAtLane(startLane)
               ?? FindDormantAtLane(startLane == CarouselLane.OffRight ? CarouselLane.OffLeft : CarouselLane.OffRight)
               ?? FindAnyInactiveSlot();
    }

    static bool IsSlotAvailableForEnter(CarouselSlot slot)
    {
        return slot.Node != null && !slot.Node.activeSelf;
    }

    CarouselSlot? FindAnyInactiveSlot()
    {
        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node != null && !slot.Node.activeSelf)
                return slot;
        }

        return null;
    }

    CarouselSlot? FindDormantAtLane(CarouselLane lane)
    {
        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node == null)
                continue;

            if (slot.Lane == lane && !slot.Node.activeSelf)
                return slot;
        }

        return null;
    }

    void PrepareEnterSlot(ref CarouselSlot slot, int tierId, CarouselLane startLane)
    {
        slot.TierId = tierId;
        slot.Lane = startLane;
        if (slot.Rt != null)
            slot.Rt.DOKill();

        slot.Node.SetActiveEx(true);
        ApplyRankDogSkeleton(slot.RankDog, tierId);
        HoldRankDogExitPose(ResolveRankDog(slot));
        SnapSlotToLane(slot, startLane);
        ApplyLockState(slot.Node, tierId);
    }

    void CompleteShift(int nextTier, int delta, bool hasEntering)
    {
        if (mShiftCompleted)
            return;

        mShiftCompleted = true;
        FinishShift(nextTier, delta, hasEntering);
    }

    void FinishShift(int nextTier, int delta, bool hasEntering)
    {
        mShiftTween = null;
        mIsAnimating = false;

        if (delta > 0)
            AdvanceLanesRight(hasEntering);
        else
            AdvanceLanesLeft(hasEntering);

        mViewTierId = nextTier;
        mEnterSlotIndex = -1;
        HideDormantSlots();
        ApplySlotDrawOrder(CarouselLane.Middle);
        PlayRankDogEnterForCurrentMiddle();
        RefreshVisibleLocks();
        UpdateButtonState();
        NotifyViewTierChanged();
        InvokeSettlementComplete();
    }

    void InvokeSettlementComplete()
    {
        var callback = mOnSettlementComplete;
        mOnSettlementComplete = null;
        callback?.Invoke();
    }

    void AdvanceLanesRight(bool hasEntering)
    {
        for (int i = 0; i < mSlots.Length; i++)
        {
            if (i == mEnterSlotIndex)
                continue;

            var slot = mSlots[i];
            switch (slot.Lane)
            {
                case CarouselLane.Left:
                    slot.Lane = CarouselLane.OffLeft;
                    slot.TierId = InvalidTier;
                    if (slot.Node != null)
                        slot.Node.SetActiveEx(false);
                    break;
                case CarouselLane.Middle:
                    slot.Lane = CarouselLane.Left;
                    break;
                case CarouselLane.Right:
                    slot.Lane = CarouselLane.Middle;
                    break;
            }

            mSlots[i] = slot;
        }

        if (hasEntering && mEnterSlotIndex >= 0)
        {
            var slot = mSlots[mEnterSlotIndex];
            slot.Lane = CarouselLane.Right;
            mSlots[mEnterSlotIndex] = slot;
        }
    }

    void AdvanceLanesLeft(bool hasEntering)
    {
        for (int i = 0; i < mSlots.Length; i++)
        {
            if (i == mEnterSlotIndex)
                continue;

            var slot = mSlots[i];
            switch (slot.Lane)
            {
                case CarouselLane.Right:
                    slot.Lane = CarouselLane.OffRight;
                    slot.TierId = InvalidTier;
                    if (slot.Node != null)
                        slot.Node.SetActiveEx(false);
                    break;
                case CarouselLane.Middle:
                    slot.Lane = CarouselLane.Right;
                    break;
                case CarouselLane.Left:
                    slot.Lane = CarouselLane.Middle;
                    break;
            }

            mSlots[i] = slot;
        }

        if (hasEntering && mEnterSlotIndex >= 0)
        {
            var slot = mSlots[mEnterSlotIndex];
            slot.Lane = CarouselLane.Left;
            mSlots[mEnterSlotIndex] = slot;
        }
    }

    void HideDormantSlots()
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node == null)
                continue;

            if (slot.Lane == CarouselLane.OffLeft || slot.Lane == CarouselLane.OffRight)
            {
                slot.TierId = InvalidTier;
                slot.Node.SetActiveEx(false);
                SnapSlotToLane(slot, slot.Lane);
                mSlots[i] = slot;
            }
        }
    }

    void RefreshVisibleLocks()
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node != null && slot.Node.activeSelf && GoldTreeRobotRankRewardHelper.IsValidTierId(slot.TierId))
                ApplyLockState(slot.Node, slot.TierId);
        }
    }

    void SnapAllSlotsToLanes()
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
            SnapSlotToLane(mSlots[i], mSlots[i].Lane);
    }

    void SnapSlotToLane(CarouselSlot slot, CarouselLane lane)
    {
        if (slot.Rt == null)
            return;

        slot.Rt.DOKill();
        slot.Rt.anchoredPosition = GetLanePosition(lane);
        float scale = GetLaneScale(lane);
        slot.Rt.localScale = new Vector3(scale, scale, scale);
    }

    Tween TweenSlotToLane(CarouselSlot slot, CarouselLane targetLane)
    {
        if (slot.Rt == null)
            return null;

        var targetPos = GetLanePosition(targetLane);
        float targetScale = GetLaneScale(targetLane);
        var targetScaleVec = new Vector3(targetScale, targetScale, targetScale);

        var tweenSeq = DOTween.Sequence();
        tweenSeq.Join(slot.Rt.DOAnchorPos(targetPos, mShiftDuration).SetEase(Ease.InOutCubic));
        tweenSeq.Join(slot.Rt.DOScale(targetScaleVec, mShiftDuration).SetEase(Ease.InOutCubic));
        return tweenSeq;
    }

    Vector2 GetLanePosition(CarouselLane lane)
    {
        switch (lane)
        {
            case CarouselLane.OffLeft: return mLayout.OffLeftPos;
            case CarouselLane.Left: return mLayout.LeftPos;
            case CarouselLane.Middle: return mLayout.MiddlePos;
            case CarouselLane.Right: return mLayout.RightPos;
            default: return mLayout.OffRightPos;
        }
    }

    static float GetLaneScale(CarouselLane lane)
    {
        return lane == CarouselLane.Middle ? CenterScale : SideScale;
    }

    void ApplySlotDrawOrder(CarouselLane topLane)
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node == null || slot.Lane == topLane)
                continue;

            slot.Node.transform.SetAsLastSibling();
        }

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node != null && slot.Lane == topLane)
                slot.Node.transform.SetAsLastSibling();
        }
    }

    static int GetPlayerLeagueTier()
    {
        return GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(
            GPUserDataManager.GPUserData.goldenTreeLeagueTier);
    }

    int GetLockReferenceTier()
    {
        if (GoldTreeRobotRankRewardHelper.IsValidTierId(mLockReferenceTier))
            return mLockReferenceTier;

        return GetPlayerLeagueTier();
    }

    void ApplyLockState(GameObject node, int displayTier)
    {
        if (node == null || !GoldTreeRobotRankRewardHelper.IsValidTierId(displayTier))
            return;

        if (ShouldHideLockAndSpineBelowFinalTier(displayTier))
        {
            SetSlotLockVisible(node, false);
            SetSlotSpineVisible(node, false);
            return;
        }

        if (mEnableInteractiveLockSpine)
        {
            ApplyInteractiveLockState(node, displayTier);
            return;
        }

        bool showLock = !mSuppressLockState && displayTier > GetLockReferenceTier();

        ApplyDogTint(node, showLock ? LockedDogColor : UnlockedDogColor);
        SetSlotLockVisible(node, showLock);
    }

    void ApplyInteractiveLockState(GameObject node, int displayTier)
    {
        int referenceTier = GetLockReferenceTier();
        bool isLocked = displayTier > referenceTier;

        ApplyDogTint(node, isLocked ? LockedDogColor : UnlockedDogColor);
        SetSlotLockVisible(node, isLocked);

        var spine = GetPassSpine(node.transform);
        if (!isLocked)
        {
            if (spine != null)
                spine.AnimationState?.ClearTracks();
            SetSlotSpineVisible(node, false);
            return;
        }

        SetSlotSpineVisible(node, true);
        if (spine != null)
            PlaySpineLoop(spine, PassIdleAnim);
    }

    void BindLockClicks()
    {
        UnbindLockClicks();
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node == null)
                continue;

            var lockTran = FindLockTransform(slot.Node.transform);
            if (lockTran == null)
                continue;

            var lockGo = lockTran.gameObject;
            var btn = lockGo.GetComponent<Button>();
            if (btn == null)
            {
                var tButton = lockGo.AddComponent<TButton>();
                tButton.transition = Selectable.Transition.None;
                tButton.clickDoTween = true;
                btn = tButton;
            }

            var slotNode = slot.Node;
            UnityAction handler = () => OnLockClicked(slotNode);
            btn.onClick.AddListener(handler);
            mLockClickBindings.Add(new LockClickBinding
            {
                Button = btn,
                Handler = handler,
            });
        }
    }

    void UnbindLockClicks()
    {
        for (int i = 0; i < mLockClickBindings.Count; i++)
        {
            var binding = mLockClickBindings[i];
            if (binding.Button != null && binding.Handler != null)
                binding.Button.onClick.RemoveListener(binding.Handler);
        }

        mLockClickBindings.Clear();
    }

    void OnLockClicked(GameObject slotNode)
    {
        if (mIsAnimating || slotNode == null)
            return;

        int slotIndex = FindSlotIndex(slotNode);
        if (slotIndex < 0)
            return;

        int tierId = mSlots[slotIndex].TierId;
        if (!GoldTreeRobotRankRewardHelper.IsValidTierId(tierId) || tierId <= GetLockReferenceTier())
            return;

        var spine = GetPassSpine(slotNode.transform);
        if (spine != null)
            PlayLockShake(spine);
    }

    static SkeletonGraphic GetPassSpine(Transform slot)
    {
        if (slot == null)
            return null;

        var spineTran = slot.Find("SkeletonGraphic (Pass_unlock)");
        return spineTran != null ? spineTran.GetComponent<SkeletonGraphic>() : null;
    }

    static SkeletonGraphic ResolveRankDog(CarouselSlot slot)
    {
        if (slot.RankDog != null)
            return slot.RankDog;

        return slot.Node != null ? GetRankDogSpine(slot.Node.transform) : null;
    }

    static SkeletonGraphic GetRankDogSpine(Transform slot)
    {
        if (slot == null)
            return null;

        var spineTran = slot.Find(RankDogNodeName);
        return spineTran != null ? spineTran.GetComponent<SkeletonGraphic>() : null;
    }

    public static void ApplyRankDogSkeleton(SkeletonGraphic spine, int tierId)
    {
        if (spine == null || !GoldTreeRobotRankRewardHelper.IsValidTierId(tierId))
            return;

        var asset = UILoadHelper.LoadAsset<SkeletonDataAsset>(GetRankDogSkeletonDataPath(tierId));
        if (asset == null)
            return;

        if (spine.skeletonDataAsset == asset && spine.IsValid)
            return;

        spine.skeletonDataAsset = asset;
        spine.initialSkinName = string.Empty;
        spine.Initialize(true);
    }

    void ApplyRankDogAnimsAtRest(bool playMiddleEnter)
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node == null || !slot.Node.activeSelf)
                continue;

            if (slot.Lane == CarouselLane.Middle)
            {
                if (playMiddleEnter)
                    PlayRankDogEnter(ResolveRankDog(slot));
                else
                    PlayRankDogIdle(ResolveRankDog(slot));
            }
            else
            {
                HoldRankDogExitPose(ResolveRankDog(slot));
            }
        }
    }

    void PlayRankDogAnimsForShift()
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node == null || !slot.Node.activeSelf)
                continue;

            if (slot.Lane == CarouselLane.Middle)
                PlayRankDogExit(ResolveRankDog(slot));
        }
    }

    void PlayRankDogEnterForCurrentMiddle()
    {
        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            var slot = mSlots[i];
            if (slot.Node == null || !slot.Node.activeSelf)
                continue;

            if (slot.Lane == CarouselLane.Middle)
                PlayRankDogEnter(ResolveRankDog(slot));
        }
    }

    void PlayRankDogEnter(SkeletonGraphic spine)
    {
        if (!TryGetRankDogState(spine, out var state))
            return;

        state.ClearTrack(0);
        var inEntry = state.SetAnimation(0, RankDogInAnim, false);
        if (inEntry != null)
        {
            inEntry.MixDuration = 0f;
            inEntry.Reverse = false;
            inEntry.TimeScale = 1f;
        }

        var idleEntry = state.AddAnimation(0, RankDogIdleAnim, true, 0f);
        if (idleEntry != null)
            idleEntry.MixDuration = 0f;

        spine.Update(0f);
    }

    void PlayRankDogIdle(SkeletonGraphic spine)
    {
        if (!TryGetRankDogState(spine, out var state))
            return;

        state.ClearTrack(0);
        var entry = state.SetAnimation(0, RankDogIdleAnim, true);
        if (entry != null)
        {
            entry.MixDuration = 0f;
            entry.Reverse = false;
            entry.TimeScale = 1f;
        }

        spine.Update(0f);
    }

    void PlayRankDogExit(SkeletonGraphic spine)
    {
        if (!TryGetRankDogState(spine, out var state))
            return;

        float reverseTrackTime = ResolveReverseStartTrackTime(state);
        state.ClearTrack(0);
        var entry = state.SetAnimation(0, RankDogInAnim, false);
        if (entry == null)
            return;

        entry.MixDuration = 0f;
        entry.MixTime = 0f;
        entry.Delay = 0f;
        entry.Reverse = true;
        entry.TimeScale = 1f;
        entry.TrackTime = reverseTrackTime;
        entry.Complete += track =>
        {
            if (track != null)
                track.TimeScale = 0f;
        };
        spine.Update(0f);
    }

    static float ResolveReverseStartTrackTime(Spine.AnimationState state)
    {
        var current = state.GetCurrent(0);
        if (current?.Animation == null || current.Animation.Name != RankDogInAnim)
            return 0f;

        if (current.Reverse)
            return current.TrackTime;

        return Mathf.Max(0f, current.AnimationEnd - current.TrackTime);
    }

    void HoldRankDogExitPose(SkeletonGraphic spine)
    {
        if (!TryGetRankDogState(spine, out var state))
            return;

        state.ClearTrack(0);
        var entry = state.SetAnimation(0, RankDogInAnim, false);
        if (entry == null)
            return;

        entry.MixDuration = 0f;
        entry.Reverse = true;
        entry.TimeScale = 0f;
        entry.TrackTime = entry.AnimationEnd;
        spine.Update(0f);
    }

    static bool TryGetRankDogState(SkeletonGraphic spine, out Spine.AnimationState state)
    {
        state = null;
        if (spine == null)
            return false;

        if (!spine.IsValid)
            spine.Initialize(false);

        state = spine.AnimationState;
        return state != null && spine.Skeleton != null;
    }

    static void ApplyDogTint(GameObject node, Color color)
    {
        if (node == null)
            return;

        var image = node.GetComponent<Image>();
        if (image != null)
            image.color = color;

        var rankDog = GetRankDogSpine(node.transform);
        if (rankDog != null)
            rankDog.color = color;
    }

    static void PlaySpineLoop(SkeletonGraphic spine, string animName)
    {
        if (spine == null || string.IsNullOrEmpty(animName))
            return;

        var animationState = spine.AnimationState;
        if (animationState == null)
            return;

        animationState.ClearTrack(0);
        animationState.SetAnimation(0, animName, true);
        spine.Update(0f);
    }

    void PlayLockShake(SkeletonGraphic spine)
    {
        if (spine == null)
            return;

        ClearActiveShakeCallback();

        var animationState = spine.AnimationState;
        if (animationState == null)
            return;

        spine.gameObject.SetActiveEx(true);
        animationState.ClearTrack(0);
        var trackEntry = animationState.SetAnimation(0, PassLockShakeAnim, false);
        if (trackEntry == null)
            return;

        mActiveShakeTrackEntry = trackEntry;
        mActiveShakeCompleteHandler = _ => PlaySpineLoop(spine, PassIdleAnim);
        trackEntry.Complete += mActiveShakeCompleteHandler;
        spine.Update(0f);
    }

    void ClearActiveShakeCallback()
    {
        if (mActiveShakeTrackEntry != null && mActiveShakeCompleteHandler != null)
            mActiveShakeTrackEntry.Complete -= mActiveShakeCompleteHandler;

        mActiveShakeTrackEntry = null;
        mActiveShakeCompleteHandler = null;
    }

    bool ShouldHideLockAndSpineBelowFinalTier(int displayTier)
    {
        return GoldTreeRobotRankRewardHelper.IsValidTierId(mHideLockSpineBelowTier)
            && displayTier < mHideLockSpineBelowTier;
    }

    static void SetSlotLockVisible(GameObject node, bool visible)
    {
        var lockTran = FindLockTransform(node.transform);
        lockTran?.gameObject.SetActiveEx(visible);
    }

    static void SetSlotSpineVisible(GameObject node, bool visible)
    {
        var spineTran = FindSpineTransform(node.transform);
        spineTran?.gameObject.SetActiveEx(visible);
    }

    static Transform FindLockTransform(Transform slot)
    {
        if (slot == null)
            return null;

        var lockTran = slot.Find("lock");
        if (lockTran != null)
            return lockTran;

        return slot.childCount > 0 ? slot.GetChild(0) : null;
    }

    static Transform FindSpineTransform(Transform slot)
    {
        if (slot == null)
            return null;

        var spineTran = slot.Find("SkeletonGraphic (Pass_unlock)");
        return spineTran != null ? spineTran : null;
    }

    void KillShiftTween()
    {
        mIsAnimating = false;

        if (mShiftTween != null && mShiftTween.IsActive())
            mShiftTween.Kill();

        mShiftTween = null;

        if (mSlots == null)
            return;

        for (int i = 0; i < mSlots.Length; i++)
        {
            if (mSlots[i].Rt != null)
                mSlots[i].Rt.DOKill();
        }
    }

    void UpdateButtonState()
    {
        if (mIsAnimating)
        {
            SetButtonsInteractable(false);
            return;
        }

        if (mBtnLeft != null)
            mBtnLeft.interactable = mViewTierId > GoldTreeRobotRankRewardHelper.MinTierId;

        if (mBtnRight != null)
            mBtnRight.interactable = mViewTierId < GoldTreeRobotRankRewardHelper.MaxTierId;
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (mBtnLeft != null)
            mBtnLeft.interactable = interactable;

        if (mBtnRight != null)
            mBtnRight.interactable = interactable;
    }
}
