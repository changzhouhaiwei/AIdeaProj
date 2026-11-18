using System.Collections.Generic;
using UnityEngine.UI;
using FishFramework;
using UnityEngine;
using Unity.Mathematics;
using FishTilePlay;
using DG.Tweening;

using System;
using System.Linq;
using TMPro;
using Spine.Unity;
using TileGamePlay;

public class UIRankChangeDlg : UIRankChangeDlgBase
{
    public static UIRankChangeDlg Instance;

    protected override string PoolObjPath => "UIRoot/UICanvas/UILayerRoot/Layer6-Panel/UIRankChangeDlg";

    public bool IsDialogShowing()
    {
        if (showState != UIPanelShowState.Idle)
            return false;

        var rt = rectTransform != null ? rectTransform : gameObject.GetComponent<RectTransform>();
        if (rt != null && rt.anchoredPosition.x <= -3000f)
            return false;

        return true;
    }

    List<IRankCell> mAllCellList = new();
    Dictionary<int, UINormalRankCell> mSlotCellByRank = new();
    List<RankData.OneUserData> mRankData;
    public Action AfterAct;
    private int myRank = -1;

    const float RowSpacing = 141f;
    const int GmTestRankListMax = 50;

    GameObject mGmTestPanel;
    TMP_InputField mGmOldRankInput;
    TMP_InputField mGmNewRankInput;
    bool mGmRankTestActive;
    RectTransform mVacatedRankPlaceholder;
    int mVacatedRankPlaceholderRank = -1;
    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == closeBtn)
        {
            // CloseSelf();
            HideMe();
            InvokeAfterAct();
        }
        else if (button == helpBtn)
        {
            UIGameFunc.OpenGoldTreeHelp();
        }
        else if (button == continueBtn)
        {
            HideMe();
            InvokeAfterAct();
        }
    }

    Sequence mContinuePop;

    protected override void OnDestroyed()
    {
        mContinuePop?.Kill();
        mContinuePop = null;
        base.OnDestroyed();
        DestroyGmTestPanel();
        ClearVacatedRankPlaceholder(true);
        if (mCurRankData != null)
            GPUserDataManager.GPUserData.goldenTreeLastRank = (int)mCurRankData.rank;
        if (mTouchCoverBtn != null)
            mTouchCoverBtn.onClick.RemoveListener(SkipRankAnimToResult);
        mTouchCoverBtn = null;
        Instance = null;
        mRankDogSpine = null;
        mDogRoot = null;
        mMiddleDogImage = null;
    }

    static bool IsGmRankTestEnabled()
        => GameModule.Setting.GmMode || GameModule.Setting.GmLevelPassGlobalSwitch;

    protected override void OnOpen()
    {
        Instance = this;

        base.OnOpen();
        HideBoxTips();
        CacheTitleTextRef();
        InitListView();
        DisableContentAutoLayout();
        EnsureMyCell();
        SetTouchEnable(false);
        SetVisible(true);
        EnsureTouchCoverSkip();

        if (!IsGmRankTestEnabled())
            boardCnt.SetActiveEx(false);
        else
            EnsureGmTestPanel();

        ShowMe();
    }

    public void HideMe()
    {
        SetVisible(false);
        if (rectTransform != null)
            rectTransform.anchoredPosition = new Vector2(-3500, 0);
        StopTimer();
        if (mCurRankData != null)
            GPUserDataManager.GPUserData.goldenTreeLastRank = (int)mCurRankData.rank;
    }

    void InvokeAfterAct()
    {
        var act = AfterAct;
        AfterAct = null;
        act?.Invoke();
    }

    Timer countTimer;
    long mPrevCountdownRemaining = -1;
    public void ShowMe()
    {
        mRankAnimSkippable = GameData.GoldTreeActData != null && GameData.GoldTreeActData.AllowRankChangeAnimSkip();
        mRankAnimPlaying = false;
        SetTouchEnable(false);
        SetVisible(true);
        gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        if (IsGmRankTestEnabled())
            EnsureGmTestPanel();

        StopTimer();
        mPrevCountdownRemaining = -1;
        UpdateCount(1.0f);
        countTimer = Interval(1.0f, UpdateCount);

        try
        {
            int addScore = GetThisLevelAddScore();
            int baselineScore = GetBaselineScore(addScore);
            mRankData = GameData.Rank.GetRankList(baselineScore);
            RefreshData();
            boardCnt.text = GPUserDataManager.GPUserData.goldenTreeBoardId.ToString(); // GameData.Rank.GetCurrentBoardId().ToString();

            RefreshLeagueTitleText();
            RefreshDogTierDisplay(playRankDogEnter: true);
            CheckPlayRankUpAnim();
        }
        catch (Exception e)
        {
            Debug.LogError($"UIRankChangeDlg ShowMe failed: {e}");
            if (!mRankAnimPlaying)
                AnimOver();
        }

        try
        {
            if (GameData.GoldTreeActData.TryMarkActivityStartReported())
                GameBAData.ExcuteActivity(0);
            // status=3：每次胜利后的排行榜结算（进度变化）
            GameBAData.ExcuteActivity(3);
            GameBAData.ExcuteGameTriggerActivity("1", "rank", 1,
                GameData.GoldTreeActData != null ? GameData.GoldTreeActData.GetActivityUniqueId() : "");
        }
        catch (Exception e)
        {
            Debug.LogError($"UIRankChangeDlg activity report failed: {e}");
        }

        if (mRankAnimPlaying)
            SetContinueVisible(false);
    }

    static int GetThisLevelAddScore()
    {
        int addScore = LevelDirector.GoldenTreeData.CurrentGoldenTreeScore;
        return math.min(addScore, 12);
    }

    static int GetBaselineScore(int addScore)
    {
        return math.max(GPUserDataManager.GPUserData.goldenTreeActPoint - addScore, 0);
    }

    public void RefreshAfterLeagueTierChanged()
    {
        if (!IsDialogShowing())
        {
            return;
        }

        mRankData = GameData.Rank.GetRankList();
        RefreshData();
        RefreshLeagueTitleText();
        RefreshDogTierDisplay(playRankDogEnter: false);
    }

    void StopTimer()
    {
        if (countTimer != null)
        {
            Unschedule(countTimer);
            countTimer = null;
        }
    }

    UINormalRankCell mMyCell;
    Timer mRankAnimSafetyTimer;
    ScrollRect mScrollRect;
    RectTransform mScrollViewport;
    float mCanonicalFirstRankSlotY = float.NaN;

    void CacheScrollRefs()
    {
        if (mScrollRect == null && scrollView != null)
            mScrollRect = scrollView.GetComponent<ScrollRect>();
        if (mScrollViewport == null && mScrollRect != null)
            mScrollViewport = mScrollRect.viewport != null ? mScrollRect.viewport : scrollView;
    }

    // 用 MyRankyCellNode 本体绑定 UINormalRankCell 作为玩家排名条，父节点保持 content。
    void EnsureMyCell()
    {
        if (mMyCell != null || myRankyCellNode == null)
            return;

        mMyCell = CreateWidget<UINormalRankCell>(
            "myRankCell", myRankyCellNode.GetComponent<UIWidgetBehaviour>());
        mMyCell.SetSelfBg();
    }

    static bool IsTop3Rank(long rank) => rank >= 1 && rank <= 3;

    static bool IsSingleRankUp(int oldRank, int newRank)
        => newRank < oldRank && oldRank - newRank == 1;

    // 玩家条抬出视口后，原名次槽位保持空白，等落回新名次时再整体下移填补。
    void VacatePlayerRankSlot(int rank)
    {
        if (rank <= 0)
            return;

        var slot = FindRankCellNode($"RankyCellNode ({rank})");
        if (slot != null)
            slot.gameObject.SetActive(false);

        if (mSlotCellByRank.TryGetValue(rank, out var slotCell)
            && slotCell != null
            && slotCell != mMyCell)
        {
            slotCell.GetRectTransform().gameObject.SetActiveEx(false);
        }

        ShowVacatedRankPlaceholder(rank);
    }

    void ShowVacatedRankPlaceholder(int rank)
    {
        if (rank <= 0 || content == null)
            return;

        var slot = FindRankCellNode($"RankyCellNode ({rank})") as RectTransform;
        if (slot == null)
            return;

        if (mVacatedRankPlaceholder == null)
        {
            var go = new GameObject("VacatedRankPlaceholder", typeof(RectTransform));
            mVacatedRankPlaceholder = go.GetComponent<RectTransform>();
            mVacatedRankPlaceholder.SetParent(content, false);
        }

        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;
        if (templateRt != null)
        {
            mVacatedRankPlaceholder.anchorMin = templateRt.anchorMin;
            mVacatedRankPlaceholder.anchorMax = templateRt.anchorMax;
            mVacatedRankPlaceholder.pivot = templateRt.pivot;
            mVacatedRankPlaceholder.sizeDelta = templateRt.sizeDelta;
        }

        mVacatedRankPlaceholder.anchoredPosition = slot.anchoredPosition;
        mVacatedRankPlaceholder.localScale = Vector3.one;
        mVacatedRankPlaceholder.gameObject.SetActive(true);
        mVacatedRankPlaceholder.SetAsLastSibling();
        mVacatedRankPlaceholderRank = rank;
    }

    void ClearVacatedRankPlaceholder(bool destroy = false)
    {
        if (mVacatedRankPlaceholder == null)
            return;

        if (destroy)
        {
            UnityEngine.Object.Destroy(mVacatedRankPlaceholder.gameObject);
            mVacatedRankPlaceholder = null;
        }
        else
        {
            mVacatedRankPlaceholder.gameObject.SetActive(false);
        }

        mVacatedRankPlaceholderRank = -1;
    }

    void ShowRankSlotNode(int rank)
    {
        if (rank <= 0)
            return;

        var slot = FindRankCellNode($"RankyCellNode ({rank})");
        if (slot != null)
            slot.gameObject.SetActive(true);
    }

    // 平滑抬出 content 并 Tween 到视口内 y 处（相对视口中心，0 为正中央）。
    void AnimateLiftMyCellToViewportPos(int vacatedRank, float targetY, float duration, Action onComplete)
    {
        CacheScrollRefs();
        if (myRankyCellNode == null || mScrollViewport == null)
        {
            onComplete?.Invoke();
            return;
        }

        VacatePlayerRankSlot(vacatedRank);

        var rt = myRankyCellNode;
        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;

        rt.DOKill();
        Vector3 worldPos = rt.position;

        if (rt.parent != mScrollViewport)
            rt.SetParent(mScrollViewport, false);

        if (templateRt != null)
        {
            rt.sizeDelta = templateRt.sizeDelta;
            rt.pivot = templateRt.pivot;
        }

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.position = worldPos;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        rt.SetActiveEx(true);

        var endPos = new Vector2(0f, targetY);
        duration = math.max(0.05f, duration);
        if (duration <= 0.05f || Vector2.Distance(rt.anchoredPosition, endPos) < 1f)
        {
            rt.anchoredPosition = endPos;
            onComplete?.Invoke();
            return;
        }

        int seq = mRankAnimSeq;
        bool completed = false;
        void Complete()
        {
            if (completed || seq != mRankAnimSeq)
                return;

            completed = true;
            onComplete?.Invoke();
        }

        var tween = rt.DOAnchorPos(endPos, duration).SetEase(Ease.OutCubic);
        tween.SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
        tween.OnComplete(Complete);
        tween.OnKill(() =>
        {
            if (!completed)
                Complete();
        });
    }

    Dictionary<int, Vector2> SnapshotRankSlotRange(int fromRank, int toRank)
    {
        var slotPos = new Dictionary<int, Vector2>();
        for (int r = fromRank; r <= toRank; r++)
        {
            var slot = FindRankCellNode($"RankyCellNode ({r})") as RectTransform;
            if (slot != null)
                slotPos[r] = slot.anchoredPosition;
        }

        return slotPos;
    }

    // 被超越行在玩家落回后下移：仅更新名次/宝箱并挪到 (r+1) 槽位。
    void ShiftOvertakenRankRows(int newRank, int oldRank, Dictionary<int, Vector2> slotPos)
    {
        if (slotPos == null)
            slotPos = SnapshotRankSlotRange(newRank, oldRank);

        for (int r = newRank; r < oldRank; r++)
        {
            if (r < 1 || r > mAllCellList.Count)
                continue;

            var cell = mAllCellList[r - 1];
            if (cell == null || cell == mMyCell)
                continue;

            if (cell is UINormalRankCell normalCell)
                normalCell.UpdateRankOnly(r + 1);

            if (!slotPos.TryGetValue(r + 1, out var target))
                continue;

            var crt = cell.GetRectTransform();
            crt.DOKill();
            crt.anchoredPosition = target;
        }
    }

    // content 使用 GoldTreeRankScrollLayout 手动锚点排行；禁用 VerticalLayoutGroup 后，
    // MyRankyCellNode 只需落到 RankyCellNode(newRank) 的锚点，被超越行移到 (r+1) 槽位锚点。
    void DropMyCellIntoContent(int newRank, int oldRank, Action onComplete = null)
    {
        AnimateDropMyCellIntoContent(newRank, oldRank, 0f, onComplete);
    }

    // 滚动结束后从视口落回 content：保留世界坐标再 Tween 到目标槽位，避免瞬移。
    void AnimateDropMyCellIntoContent(int newRank, int oldRank, float duration, Action onComplete)
    {
        if (myRankyCellNode == null)
        {
            onComplete?.Invoke();
            return;
        }

        ClearVacatedRankPlaceholder();

        var slotPos = SnapshotRankSlotRange(newRank, oldRank);
        Vector2 targetPos = slotPos.TryGetValue(newRank, out var landPos)
            ? landPos
            : GetSlotAnchoredPos(newRank);

        var rt = myRankyCellNode;
        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;

        rt.DOKill();
        Vector3 worldPos = rt.position;

        if (rt.parent != content)
            rt.SetParent(content, false);

        if (templateRt != null)
        {
            rt.anchorMin = templateRt.anchorMin;
            rt.anchorMax = templateRt.anchorMax;
            rt.pivot = templateRt.pivot;
            rt.sizeDelta = templateRt.sizeDelta;
        }

        rt.position = worldPos;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        rt.SetActiveEx(true);

        int seq = mRankAnimSeq;
        bool completed = false;
        void Complete()
        {
            if (completed || seq != mRankAnimSeq)
                return;

            completed = true;
            ShiftOvertakenRankRows(newRank, oldRank, slotPos);
            rt.anchoredPosition = targetPos;
            onComplete?.Invoke();
        }

        duration = math.max(0f, duration);
        if (duration <= 0.05f || Vector2.Distance(rt.anchoredPosition, targetPos) < 1f)
        {
            Complete();
            return;
        }

        duration = math.max(0.05f, duration);
        var tween = rt.DOAnchorPos(targetPos, duration).SetEase(Ease.InOutCubic);
        tween.SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
        tween.OnComplete(Complete);
        tween.OnKill(() =>
        {
            if (!completed)
                Complete();
        });
    }

    // 升 1 名：玩家先移到新名次，落回后再下移被超越行；原名次槽位在移动过程中保持空白。
    void AnimateMyCellRankUpInContent(int newRank, int oldRank, float duration, Action onComplete)
    {
        if (myRankyCellNode == null)
        {
            onComplete?.Invoke();
            return;
        }

        TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.rankUp);

        var slotPos = SnapshotRankSlotRange(newRank, oldRank);
        VacatePlayerRankSlot(oldRank);

        var rt = myRankyCellNode;
        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;

        if (rt.parent != content)
            rt.SetParent(content, false);

        if (templateRt != null)
        {
            rt.anchorMin = templateRt.anchorMin;
            rt.anchorMax = templateRt.anchorMax;
            rt.pivot = templateRt.pivot;
            rt.sizeDelta = templateRt.sizeDelta;
        }

        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        rt.SetActiveEx(true);

        Vector2 targetPos = slotPos.TryGetValue(newRank, out var landPos)
            ? landPos
            : GetSlotAnchoredPos(newRank);

        rt.DOKill();
        duration = math.max(0.05f, duration);

        int seq = mRankAnimSeq;
        bool completed = false;
        void Complete()
        {
            if (completed || seq != mRankAnimSeq)
                return;

            completed = true;
            ClearVacatedRankPlaceholder();
            ShiftOvertakenRankRows(newRank, oldRank, slotPos);
            rt.anchoredPosition = targetPos;
            onComplete?.Invoke();
        }

        var sequence = DOTween.Sequence();
        sequence.Append(rt.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCubic));
        sequence.AppendCallback(Complete);
        sequence.SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
        sequence.OnKill(() =>
        {
            if (!completed)
                Complete();
        });
        sequence.Play();
    }

    // 将 MyRankyCellNode 从 viewport 落回 content 指定名次槽位（无动画）。
    void RestoreMyCellToContentAtRank(int rank)
    {
        if (myRankyCellNode == null || rank <= 0)
            return;

        var rt = myRankyCellNode;
        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;

        if (rt.parent != content)
            rt.SetParent(content, false);

        if (templateRt != null)
        {
            rt.anchorMin = templateRt.anchorMin;
            rt.anchorMax = templateRt.anchorMax;
            rt.pivot = templateRt.pivot;
            rt.sizeDelta = templateRt.sizeDelta;
        }

        rt.anchoredPosition = GetSlotAnchoredPos(rank);
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        rt.SetActiveEx(true);
    }

    // content 已到达目标滚动位置（含顶部 clamp），无法继续滚动。
    bool CannotScrollFurtherUp(Vector2 dstPos)
    {
        CacheScrollRefs();
        if (mScrollRect == null || mScrollViewport == null || mScrollRect.content == null)
            return true;

        float viewportHeight = mScrollViewport.rect.height;
        float clampedDstY = ClampContentScrollY(dstPos.y, viewportHeight);
        float currentY = mScrollRect.content.anchoredPosition.y;
        return Mathf.Abs(currentY - clampedDstY) < 0.01f;
    }

    // 基线布局下某名次槽位在 content 中的锚点位置。
    Vector2 GetSlotAnchoredPos(int rank)
    {
        if (rank >= 1 && rank <= mAllCellList.Count)
        {
            var cell = mAllCellList[rank - 1];
            if (cell != null)
                return cell.GetRectTransform().anchoredPosition;
        }

        var slot = FindRankCellNode($"RankyCellNode ({rank})") as RectTransform;
        if (slot != null)
            return slot.anchoredPosition;

        var firstRow = FindRankCellNode("RankyCellNode (1)") as RectTransform;
        if (firstRow != null)
        {
            var pos = firstRow.anchoredPosition;
            pos.y -= (rank - 1) * RowSpacing;
            return pos;
        }

        return Vector2.zero;
    }

    Transform mRankUpArriveOriginalParent;

    void CacheRankUpEffectParents()
    {
        if (myRankUpArrive != null && mRankUpArriveOriginalParent == null)
            mRankUpArriveOriginalParent = myRankUpArrive.parent;
    }

    static void SetupRankUpEffectTransform(RectTransform effectRt)
    {
        if (effectRt == null)
            return;

        effectRt.anchorMin = new Vector2(0.5f, 0.5f);
        effectRt.anchorMax = new Vector2(0.5f, 0.5f);
        effectRt.pivot = new Vector2(0.5f, 0.5f);
        effectRt.anchoredPosition = Vector2.zero;
        effectRt.localScale = Vector3.one;
    }

    void RestoreRankUpArriveParent()
    {
        if (myRankUpArrive == null || mRankUpArriveOriginalParent == null)
            return;

        myRankUpArrive.SetParent(mRankUpArriveOriginalParent, false);
        SetupRankUpEffectTransform(myRankUpArrive);
    }

    void ResetRankUpEffects()
    {
        myRankUpEff.SetActiveEx(false);
        myRankUpArrive.SetActiveEx(false);
    }

    void ShowRankUpArriveEffect()
    {
        RestoreRankUpArriveParent();
        myRankUpEff.SetActiveEx(false);
        myRankUpArrive.SetActiveEx(false);
        myRankUpArrive.SetActiveEx(true);
    }

    void ShowRankUpArriveEffectAtCell(UINormalRankCell cell)
    {
        CacheRankUpEffectParents();
        myRankUpEff.SetActiveEx(false);
        myRankUpArrive.SetActiveEx(false);

        if (cell != null)
        {
            var cellRt = cell.GetRectTransform();
            myRankUpArrive.SetParent(cellRt, false);
            SetupRankUpEffectTransform(myRankUpArrive);
            myRankUpArrive.SetAsLastSibling();
        }

        myRankUpArrive.SetActiveEx(true);
        TryPlayArrivedTop3JpPop();
    }

    void HideRankUpEffects()
    {
        myRankUpEff.SetActiveEx(false);
        myRankUpArrive.SetActiveEx(false);
        RestoreRankUpArriveParent();
    }

    RankData.OneUserData mLastRankData;
    RankData.OneUserData mCurRankData;

    void PlayShowAddNumAnim(UINormalRankCell cell, int addNum, Action action)
    {
        int seq = mRankAnimSeq;
        if (rankTreeCollect == null)
        {
            action?.Invoke();
            return;
        }

        cell.PlayAddTreeAnim(rankTreeCollect, addNum, () =>
        {
            if (seq != mRankAnimSeq)
                return;

            action?.Invoke();
        });
        DelayIfRankSeq(seq, 1.0f, () =>
            TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.coinAdd));
    }

    const float RankAddAnimDuration = 2.0f;
    const float RankUpPreScrollDelay = 0.4f;
    const float RankUpLiftDuration = 0.32f;
    const float RankUpDropDuration = 0.32f;
    const float RankUpArrivePreDelay = 0.2f;
    const float RankUpLandDelay = 1.0f;
    const float RankAnimSafetyBuffer = 2f;

    bool mRankAnimPlaying;
    bool mRankAnimSkippable;
    int mRankAnimSeq;
    bool mForceRankLayout;
    Button mTouchCoverBtn;

    void PlayRankUpAnim(RankData.OneUserData lastData, RankData.OneUserData curData, int addScore)
    {
        mRankAnimSeq++;
        mRankAnimPlaying = true;
        SetTouchEnable(false);
        if (mRankAnimSafetyTimer != null)
            Unschedule(mRankAnimSafetyTimer);

        ResetRankUpEffects();
        mLastRankData = new RankData.OneUserData(lastData);
        mCurRankData = new RankData.OneUserData(curData);
        RefreshTitleText();
        ScheduleRankAnimSafetyTimer();
        PlayRankChangeSequence(addScore);
    }

    //播放排名变化动画
    void CheckPlayRankUpAnim()
    {
        try
        {
            int addScore = GetThisLevelAddScore();
            int baselineScore = GetBaselineScore(addScore);

            var baselineMyData = GameData.Rank.GetMyRankData(baselineScore);
            var finalMyData = GameData.Rank.GetMyRankData();
            if (baselineMyData == null || finalMyData == null)
            {
                AnimOver();
                return;
            }

            PlayRankUpAnim(baselineMyData, finalMyData, addScore);
        }
        catch (Exception e)
        {
            Debug.LogError($"UIRankChangeDlg rank anim failed: {e}");
            mRankAnimPlaying = false;
            AnimOver();
        }
    }

    public void PlayGmRankChangeTest(int oldRank, int newRank)
    {
        if (!IsGmRankTestEnabled())
            return;

        var source = GameData.Rank.GetRankList();
        if (!IsValidGmTestRank(oldRank, source.Count)
            || !IsValidGmTestRank(newRank, source.Count))
        {
            Debug.LogWarning(
                $"[UIRankChangeDlg] Invalid GM rank test: old={oldRank}, new={newRank}, valid=[1,{source.Count}]");
            return;
        }

        mGmRankTestActive = true;
        mRankData = BuildGmTestRankList(oldRank, newRank);
        if (mRankData == null || mRankData.Count == 0)
        {
            mGmRankTestActive = false;
            return;
        }

        PrepareGmRankChangePlayback(oldRank);
        RefreshData();
        PositionMyCellAtRank(oldRank);

        if (!TryMakeGmRankUserData(oldRank, newRank, out var lastData, out var curData, out int addScore))
        {
            mGmRankTestActive = false;
            return;
        }

        try
        {
            PlayRankUpAnim(lastData, curData, addScore);
        }
        catch (Exception e)
        {
            mGmRankTestActive = false;
            Debug.LogError($"UIRankChangeDlg GM rank anim failed: {e}");
            mRankAnimPlaying = false;
            AnimOver();
        }
    }

    static bool IsValidGmTestRank(int rank, int listCount)
        => rank >= 1 && rank <= listCount;

    bool TryGetGmRankScores(int oldRank, int newRank, out long oldScore, out long newScore)
    {
        oldScore = 0;
        newScore = 0;
        var source = GameData.Rank.GetRankList();
        if (!IsValidGmTestRank(oldRank, source.Count) || !IsValidGmTestRank(newRank, source.Count))
            return false;

        oldScore = source[oldRank - 1].score;
        newScore = source[newRank - 1].score;
        return true;
    }

    static int GetGmAnimAddScore(long oldScore, long newScore)
    {
        int addScore = (int)(newScore - oldScore);
        if (addScore < 0)
            return math.max(1, math.abs(addScore));

        return addScore;
    }

    List<RankData.OneUserData> BuildGmRankListWithPlayerAt(
        List<RankData.OneUserData> source, int playerRank, long playerScore)
    {
        string uid = GPUserDataManager.Instance.GetUID();
        var robots = source
            .Where(v => !v.playerId.Equals(uid))
            .Select(v => new RankData.OneUserData(v))
            .ToList();

        var result = new List<RankData.OneUserData>();
        int robotIdx = 0;
        int slotCount = math.min(GmTestRankListMax, source.Count);
        for (int rank = 1; rank <= slotCount; rank++)
        {
            if (rank == playerRank)
            {
                result.Add(new RankData.OneUserData
                {
                    playerId = uid,
                    playerName = GPUserDataManager.GPUserData.username,
                    pictureUrl = GPUserDataManager.GPUserData.userhead.ToString(),
                    score = playerScore,
                    rank = rank
                });
            }
            else if (robotIdx < robots.Count)
            {
                var bot = robots[robotIdx++];
                bot.rank = rank;
                result.Add(bot);
            }
        }

        return result;
    }

    List<RankData.OneUserData> BuildGmTestRankList(int oldRank, int newRank)
    {
        if (!TryGetGmRankScores(oldRank, newRank, out long oldScore, out _))
            return null;

        var source = GameData.Rank.GetRankList()
            .Select(v => new RankData.OneUserData(v))
            .ToList();
        return BuildGmRankListWithPlayerAt(source, oldRank, oldScore);
    }

    bool TryMakeGmRankUserData(
        int oldRank, int newRank,
        out RankData.OneUserData lastData, out RankData.OneUserData curData, out int addScore)
    {
        lastData = null;
        curData = null;
        addScore = 0;
        if (!TryGetGmRankScores(oldRank, newRank, out long oldScore, out long newScore))
            return false;

        string uid = GPUserDataManager.Instance.GetUID();
        lastData = new RankData.OneUserData
        {
            playerId = uid,
            playerName = GPUserDataManager.GPUserData.username,
            pictureUrl = GPUserDataManager.GPUserData.userhead.ToString(),
            score = oldScore,
            rank = oldRank
        };
        curData = new RankData.OneUserData(lastData)
        {
            score = newScore,
            rank = newRank
        };
        addScore = GetGmAnimAddScore(oldScore, newScore);
        return true;
    }

    void ApplyGmRankTestResult()
    {
        int newRank = (int)mCurRankData.rank;
        int newScore = (int)mCurRankData.score;
        GPUserDataManager.GPUserData.GmSetGoldTreeActPoint(newScore);
        GPUserDataManager.GPUserData.goldenTreeLastRank = newRank;
        GPUserDataManager.Instance.SaveData();
        // 同分时 GetRankList 会把玩家顶到前面；GM 测试按面板设定名次落位，不跟 CompareRankOrder 走。
        var source = GameData.Rank.GetRankList(newScore)
            .Select(v => new RankData.OneUserData(v))
            .ToList();
        mRankData = BuildGmRankListWithPlayerAt(source, newRank, newScore);
        mGmRankTestActive = false;
        RefreshTitleText();
        RefreshLeagueTitleText();
    }

    void PrepareGmRankChangePlayback(int oldRank)
    {
        if (mRankAnimSafetyTimer != null)
        {
            Unschedule(mRankAnimSafetyTimer);
            mRankAnimSafetyTimer = null;
        }

        HideRankUpEffects();
        KillAllRankAnimTweens();
        ClearVacatedRankPlaceholder();

        CacheScrollRefs();
        if (myRankyCellNode != null && mScrollViewport != null && myRankyCellNode.parent == mScrollViewport)
            RestoreMyCellToContentAtRank(oldRank);
    }

    void KillAllRankAnimTweens()
    {
        myRankyCellNode?.DOKill();
        content?.DOKill();
        CacheScrollRefs();
        mScrollRect?.content?.DOKill();
        foreach (var cell in mAllCellList)
            cell?.GetRectTransform()?.DOKill();
    }

    RectTransform GetGmTestPanelParent()
    {
        if (rectTransform != null)
            return rectTransform;
        if (safeArea != null)
            return safeArea;
        return gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
    }

    void BringGmTestPanelToFront()
    {
        if (mGmTestPanel == null)
            return;

        mGmTestPanel.transform.SetAsLastSibling();
    }

    void EnsureGmTestPanel()
    {
        if (!IsGmRankTestEnabled())
            return;

        if (mGmTestPanel != null)
        {
            mGmTestPanel.SetActiveEx(true);
            BringGmTestPanelToFront();
            return;
        }

        var parent = GetGmTestPanelParent();
        if (parent == null)
        {
            Debug.LogWarning("[UIRankChangeDlg] GM test panel parent is null.");
            return;
        }

        var font = timeLb != null ? timeLb.font : null;
        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            Debug.LogWarning("[UIRankChangeDlg] GM test panel skipped: TMP font missing.");
            return;
        }

        try
        {
            mGmTestPanel = new GameObject("GmRankTestPanel", typeof(RectTransform));
            mGmTestPanel.transform.SetParent(parent, false);
            var panelRt = mGmTestPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(0f, 132f);
            panelRt.sizeDelta = new Vector2(0f, 72f);

            var panelImage = mGmTestPanel.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.82f);
            panelImage.raycastTarget = true;

            CreateGmLabel("GM名次测试", panelRt, font, new Vector2(12f, -36f), new Vector2(120f, 32f));

            float x = 12f;
            const float fieldWidth = 56f;
            const float fieldHeight = 36f;
            const float labelWidth = 28f;

            CreateGmLabel("旧", panelRt, font, new Vector2(x, -10f), new Vector2(labelWidth, fieldHeight));
            x += labelWidth;
            mGmOldRankInput = CreateGmTmpInput("OldRank", panelRt, font, "8", new Vector2(x, 0f), new Vector2(fieldWidth, fieldHeight));
            x += fieldWidth + 12f;

            CreateGmLabel("新", panelRt, font, new Vector2(x, -10f), new Vector2(labelWidth, fieldHeight));
            x += labelWidth;
            mGmNewRankInput = CreateGmTmpInput("NewRank", panelRt, font, "7", new Vector2(x, 0f), new Vector2(fieldWidth, fieldHeight));
            x += fieldWidth + 12f;

            CreateGmPlayButton(panelRt, font, new Vector2(x, 0f), new Vector2(72f, fieldHeight));
            BringGmTestPanelToFront();
        }
        catch (Exception e)
        {
            Debug.LogError($"[UIRankChangeDlg] Create GM test panel failed: {e}");
            DestroyGmTestPanel();
        }
    }

    static void CreateGmLabel(string text, RectTransform parent, TMP_FontAsset font, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Label_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.text = text;
    }

    static TMP_InputField CreateGmTmpInput(
        string name, RectTransform parent, TMP_FontAsset font, string defaultText, Vector2 pos, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);
        rootRt.anchoredPosition = pos;
        rootRt.sizeDelta = size;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.92f);

        var textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(root.transform, false);
        var textAreaRt = textArea.GetComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(8f, 4f);
        textAreaRt.offsetMax = new Vector2(-8f, -4f);
        textArea.AddComponent<RectMask2D>();

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGo.transform.SetParent(textArea.transform, false);
        var placeholderRt = placeholderGo.GetComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = Vector2.zero;
        placeholderRt.offsetMax = Vector2.zero;
        var placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholder.font = font;
        placeholder.fontSize = 20f;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
        placeholder.text = defaultText;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(textArea.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 20f;
        text.color = Color.black;

        var input = root.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRt;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.text = defaultText;
        return input;
    }

    void CreateGmPlayButton(RectTransform parent, TMP_FontAsset font, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("PlayBtn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.2f, 0.65f, 0.25f, 1f);

        var btn = go.AddComponent<TButton>();
        btn.targetGraphic = image;
        btn.transition = Selectable.Transition.None;
        btn.clickDoTween = true;
        btn.onClick.AddListener(OnGmPlayRankTestClicked);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var label = textGo.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = "播放";
    }

    void OnGmPlayRankTestClicked()
    {
        if (!IsGmRankTestEnabled())
            return;

        int oldRank = ParseGmInput(mGmOldRankInput, 8);
        int newRank = ParseGmInput(mGmNewRankInput, 7);
        PlayGmRankChangeTest(oldRank, newRank);
    }

    static int ParseGmInput(TMP_InputField input, int fallback)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return fallback;

        return int.TryParse(input.text.Trim(), out int value) ? value : fallback;
    }

    void DestroyGmTestPanel()
    {
        if (mGmTestPanel == null)
            return;

        var playBtn = mGmTestPanel.transform.Find("PlayBtn")?.GetComponent<Button>();
        if (playBtn != null)
            playBtn.onClick.RemoveListener(OnGmPlayRankTestClicked);

        UnityEngine.Object.Destroy(mGmTestPanel);
        mGmTestPanel = null;
        mGmOldRankInput = null;
        mGmNewRankInput = null;
    }

    // MyRankyCellNode 流程：旧名次可见 -> 抬到视口中央（加分）-> 滚动 content -> 落回最终名次并下移其余行。
    void PlayRankChangeSequence(int addScore)
    {
        int seq = mRankAnimSeq;
        int oldRank = (int)mLastRankData.rank;
        int newRank = (int)mCurRankData.rank;

        // 1. 初始展示：MyRankyCellNode 在 content 旧名次槽位，滚动使其可见。
        ScrollToRankStatic(oldRank);
        if (mMyCell != null)
            mMyCell.UpdateData(mLastRankData);

        // 名次不变：不抬出 content、不滚动，仅原地播放加分动画。但仍要保证 MyRankyCellNode 在 content 中的正确次序。
        if (newRank >= oldRank)
        {
            DelayIfRankSeq(seq, RankUpPreScrollDelay, () =>
            {
                PlayShowAddNumAnim(mMyCell, addScore, () =>
                {
                    mMyCell.UpdateData(mCurRankData);
                    // newRank == oldRank：DropMyCellIntoContent 只做 content 内的名次次序归位（无移出/移入、无行下移）。
                    DropMyCellIntoContent(newRank, oldRank);
                    ShowRankUpArriveEffectAtCell(mMyCell);
                    DelayIfRankSeq(seq, RankUpLandDelay, FinishRankAnim);
                });
            });
            return;
        }

        if (IsSingleRankUp(oldRank, newRank))
        {
            DelayIfRankSeq(seq, RankUpPreScrollDelay, () =>
            {
                PlayShowAddNumAnim(mMyCell, addScore, () =>
                {
                    mMyCell.UpdateData(mCurRankData);
                    float moveDuration = GetRankUpScrollDuration(1);
                    AnimateMyCellRankUpInContent(newRank, oldRank, moveDuration, () =>
                    {
                        ShowRankUpArriveEffectAtCell(mMyCell);
                        DelayIfRankSeq(seq, RankUpLandDelay, FinishRankAnim);
                    });
                });
            });
            return;
        }

        DelayIfRankSeq(seq, RankUpPreScrollDelay, () =>
        {
            CacheScrollRefs();
            var dstPos = GetDstAnchoredPos(newRank);
            int rankUpCount = Math.Max(0, oldRank - newRank);

            void OnRankUpMoveComplete()
            {
                ShowRankUpArriveEffectAtCell(mMyCell);
                DelayIfRankSeq(seq, RankUpLandDelay, FinishRankAnim);
            }

            // 2. 平滑抬到视口正中，再播放加分（原名次槽位留空，落回时再下移填补）。
            AnimateLiftMyCellToViewportPos(oldRank, 0f, RankUpLiftDuration, () =>
            {
                // 3. 固定处播放加分动画，数据 旧->新。
                PlayShowAddNumAnim(mMyCell, addScore, () =>
                {
                    mMyCell.UpdateData(mCurRankData);

                    // 滚动过程中若已无法继续滚动，改为 content 内直接位移。
                    if (CannotScrollFurtherUp(dstPos))
                    {
                        ClearVacatedRankPlaceholder();
                        ShowRankSlotNode(oldRank);
                        RestoreMyCellToContentAtRank(oldRank);
                        float moveDuration = GetRankUpScrollDuration(rankUpCount);
                        AnimateMyCellRankUpInContent(newRank, oldRank, moveDuration, OnRankUpMoveComplete);
                        return;
                    }

                    // 4. 滚动 content 使最终名次落点对齐固定位置。
                    PlayScrollViewUpAnim(dstPos, rankUpCount, () =>
                    {
                        // 5. 从视口平滑落回 content，再下移被超越行。
                        AnimateDropMyCellIntoContent(newRank, oldRank, RankUpDropDuration, OnRankUpMoveComplete);
                    });
                });
            });
        });
    }

    void ScheduleRankAnimSafetyTimer()
    {
        if (mRankAnimSafetyTimer != null)
            Unschedule(mRankAnimSafetyTimer);

        float timeout = 8f;
        if (mCurRankData.rank < mLastRankData.rank)
        {
            int oldRank = (int)mLastRankData.rank;
            int newRank = (int)mCurRankData.rank;
            int rankUpCount = oldRank - newRank;
            float moveDuration = IsSingleRankUp(oldRank, newRank)
                ? GetRankUpScrollDuration(1)
                : GetRankUpScrollDuration(rankUpCount);
            float liftDuration = IsSingleRankUp(oldRank, newRank) ? 0f : RankUpLiftDuration;
            float dropDuration = IsSingleRankUp(oldRank, newRank) ? 0f : RankUpDropDuration;
            timeout = RankUpPreScrollDelay + liftDuration + RankAddAnimDuration
                + moveDuration + dropDuration + RankUpLandDelay + RankAnimSafetyBuffer;
        }

        mRankAnimSafetyTimer = Delay(timeout, FinishRankAnim);
    }

    UIRankChangeDlgScrollSettings mScrollSettings;

    void CacheScrollSettings()
    {
        if (mScrollSettings == null && viewBehaviour != null)
            mScrollSettings = viewBehaviour.GetComponent<UIRankChangeDlgScrollSettings>();
    }

    void PlayScrollViewUpAnim(Vector2 dstPos, int rankUpCount, Action action)
    {
        CacheScrollRefs();
        var rect = mScrollRect != null ? mScrollRect : scrollView.GetComponent<ScrollRect>();
        if (rect == null || rect.content == null)
        {
            action?.Invoke();
            return;
        }

        TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.rankUp);

        rect.StopMovement();
        rect.content.DOKill();
        CacheScrollSettings();

        float scrollDuration = GetRankUpScrollDuration(rankUpCount);
        scrollDuration = math.max(0.05f, scrollDuration);

        myRankUpEff.SetActiveEx(false);
        myRankUpEff.SetActiveEx(true);

        rect.enabled = false;
        int seq = mRankAnimSeq;
        bool scrollCompleted = false;
        void CompleteScroll()
        {
            if (scrollCompleted || seq != mRankAnimSeq)
                return;

            scrollCompleted = true;
            rect.enabled = true;
            myRankUpEff.SetActiveEx(false);
            action?.Invoke();
        }

        var sequece = DOTween.Sequence();
        var tw1 = rect.content.DOAnchorPos(dstPos, scrollDuration).SetEase(Ease.Linear);
        sequece.Append(tw1);
        sequece.AppendCallback(CompleteScroll);
        sequece.SetLink(rect.content.gameObject, LinkBehaviour.KillOnDestroy);
        sequece.OnKill(() =>
        {
            if (!scrollCompleted)
                CompleteScroll();
        });
        sequece.Play();
    }

    /// <summary>
    /// Scroll View 滚动时长 = 上升名次 / 曲线速度（名次/秒）。
    /// 不含加分动画开始前的等待，也不含滚动结束后的落下表现。
    /// </summary>
    float GetRankUpScrollDuration(int rankUpCount)
    {
        CacheScrollSettings();
        if (mScrollSettings != null)
            return mScrollSettings.GetScrollDuration(rankUpCount);

        rankUpCount = math.max(1, rankUpCount);
        return rankUpCount / 10f;
    }


    void FinishRankAnim()
    {
        bool forceLayout = mForceRankLayout;
        mForceRankLayout = false;
        mRankAnimPlaying = false;
        AnimOver();
        HideRankUpEffects();
        bool gmLayoutResync = mGmRankTestActive;
        if (mGmRankTestActive)
            ApplyGmRankTestResult();
        else
            mRankData = GameData.Rank.GetRankList();
        RefreshRankDataOnly(forceLayout || gmLayoutResync);
        RefreshMyRankCellBg();
        if (forceLayout)
            ScrollToMyNode();
    }

    void TryPlayArrivedTop3JpPop()
    {
        if (mMyCell == null || mLastRankData == null || mCurRankData == null)
            return;
        if (mCurRankData.rank >= mLastRankData.rank)
            return;
        if (!IsTop3Rank(mCurRankData.rank))
            return;

        mMyCell.SetSelfBg();
        mMyCell.PlayTop3JpPop();
    }

    void RefreshMyRankCellBg()
    {
        mMyCell?.SetSelfBg();
    }

    void AnimOver()
    {
        if (mRankAnimPlaying)
            return;

        if (mRankAnimSafetyTimer != null)
        {
            Unschedule(mRankAnimSafetyTimer);
            mRankAnimSafetyTimer = null;
        }

        SetTouchEnable(true);
        SetContinueVisible(true);
        if (IsGmRankTestEnabled())
            EnsureGmTestPanel();
        else
            BringGmTestPanelToFront();
    }

    void UpdateCount(float tm)
    {
        var nowServerTime = GameData.GetServerTime();
        var endTime = GameData.GoldTreeActData.GetEndTime();
        var deltaTime = endTime - nowServerTime;
        long remaining = GameData.GoldTreeActData.GetCountdownRemaining();
        GameData.GoldTreeActData.TryReportActivityCountdownEnded(mPrevCountdownRemaining, remaining);
        if (remaining >= 0)
            mPrevCountdownRemaining = remaining;
        deltaTime = math.max(0, deltaTime);
        timeLb.text = FishFramework.Utility.GetHourMinSec(deltaTime);
    }


    void RefreshData()
    {
        EnsureMyCell();
        if (mRankData == null)
            return;

        string uid = GPUserDataManager.Instance.GetUID();
        int count = mRankData.Count;

        myRank = -1;
        for (int i = 0; i < count; i++)
        {
            if (mRankData[i].playerId.Equals(uid))
            {
                myRank = i + 1;
                break;
            }
        }

        // 以名次为索引重建列表：玩家名次槽位由 MyRankyCellNode 表示，其余名次用普通格子。
        mAllCellList.Clear();
        for (int rank = 1; rank <= count; rank++)
        {
            var item = mRankData[rank - 1];
            if (rank == myRank && mMyCell != null)
            {
                EnsureRankSlotNode(rank);
                mMyCell.UpdateData(item);
                mMyCell.SetSelfBg();
                mAllCellList.Add(mMyCell);
            }
            else
            {
                var cell = GetOrCreateSlotCell(rank);
                cell.UpdateData(item);
                cell.SetDefaultBg();
                mAllCellList.Add(cell);
            }
        }

        foreach (var cell in mAllCellList)
            cell.SetBtnClickCb(OnCellAwardClick);

        FitScrollContentHeight();
        PositionMyCellAtRank(myRank);
    }

    // 升榜动画结束后同步榜单：升名次时保留动画落点，仅同步数据；非升榜才重排布局。
    void RefreshRankDataOnly(bool forceLayoutResync = false)
    {
        EnsureMyCell();
        if (mRankData == null)
            return;

        string uid = GPUserDataManager.Instance.GetUID();
        int count = mRankData.Count;
        int oldRank = mLastRankData != null ? (int)mLastRankData.rank : -1;
        var prevCells = new List<IRankCell>(mAllCellList);

        myRank = -1;
        for (int i = 0; i < count; i++)
        {
            if (mRankData[i].playerId.Equals(uid))
            {
                myRank = i + 1;
                break;
            }
        }

        bool rankUp = oldRank > myRank && myRank > 0;
        bool resyncLayout = forceLayoutResync || !rankUp;
        var cellByPlayerId = BuildCellByPlayerIdMap();
        Dictionary<int, Vector2> slotPos = null;

        if (resyncLayout)
        {
            ResetRankSlotAnchorLayout(count);
            slotPos = SnapshotRankSlotPositions(count);
        }

        mAllCellList.Clear();
        for (int rank = 1; rank <= count; rank++)
        {
            var item = mRankData[rank - 1];
            if (rank == myRank && mMyCell != null)
            {
                mMyCell.TryRefreshRankData(item);
                mMyCell.SetSelfBg();
                mAllCellList.Add(mMyCell);
                continue;
            }

            var cell = ResolveCellWidgetAfterRankUp(
                rank, oldRank, myRank, prevCells, cellByPlayerId, item.playerId);
            if (cell != null)
            {
                ApplyPostAnimCellData(cell, item);
                if (resyncLayout && slotPos != null && slotPos.TryGetValue(rank, out var pos))
                    SnapCellToRankPos(cell, pos);
                mAllCellList.Add(cell);
                continue;
            }

            var slotCell = GetOrCreateSlotCell(rank);
            slotCell.UpdateData(item);
            slotCell.SetDefaultBg();
            if (resyncLayout && slotPos != null && slotPos.TryGetValue(rank, out var fallbackPos))
                SnapCellToRankPos(slotCell, fallbackPos);
            mAllCellList.Add(slotCell);
        }

        foreach (var cell in mAllCellList)
            cell.SetBtnClickCb(OnCellAwardClick);

        if (resyncLayout && myRank > 0)
            PositionMyCellAtRank(myRank, slotPos);
    }

    Dictionary<int, Vector2> SnapshotRankSlotPositions(int count)
    {
        var slotPos = new Dictionary<int, Vector2>(count);
        for (int rank = 1; rank <= count; rank++)
        {
            var slot = FindRankCellNode($"RankyCellNode ({rank})") as RectTransform;
            if (slot != null)
                slotPos[rank] = slot.anchoredPosition;
        }

        return slotPos;
    }

    void SnapCellToRankPos(IRankCell cell, Vector2 pos)
    {
        if (cell == null)
            return;

        var rt = cell.GetRectTransform();
        rt.DOKill();
        rt.anchoredPosition = pos;
        rt.gameObject.SetActiveEx(true);
    }

    // 升榜后按动画前的 widget 连续性取格子（下移行沿用原 widget，避免头像被换）。
    UINormalRankCell ResolveCellWidgetAfterRankUp(
        int rank,
        int oldRank,
        int newRank,
        List<IRankCell> prevCells,
        Dictionary<string, UINormalRankCell> cellByPlayerId,
        string targetPlayerId)
    {
        if (oldRank > newRank && newRank > 0 && prevCells.Count > 0)
        {
            int prevIndex = rank > newRank && rank <= oldRank ? rank - 2 : rank - 1;
            if (prevIndex >= 0 && prevIndex < prevCells.Count)
            {
                var prev = prevCells[prevIndex];
                if (prev != null && prev != mMyCell && prev is UINormalRankCell prevCell)
                    return prevCell;
            }
        }
        else if (rank - 1 >= 0 && rank - 1 < prevCells.Count)
        {
            var prev = prevCells[rank - 1];
            if (prev != null && prev != mMyCell && prev is UINormalRankCell prevCell)
                return prevCell;
        }

        if (!string.IsNullOrEmpty(targetPlayerId)
            && cellByPlayerId.TryGetValue(targetPlayerId, out var mapped))
            return mapped;

        return null;
    }

    static void ApplyPostAnimCellData(UINormalRankCell cell, RankData.OneUserData item)
    {
        if (cell == null || item == null)
            return;

        var cellPlayerId = cell.GetPlayerId();
        if (!string.IsNullOrEmpty(cellPlayerId) && cellPlayerId.Equals(item.playerId))
        {
            cell.TrySyncRankAfterAnim(item);
            return;
        }

        // 动画 widget 与榜单 playerId 不一致时也只改名次/宝箱，不换头像。
        cell.UpdateRankOnly((int)item.rank);
    }

    Dictionary<string, UINormalRankCell> BuildCellByPlayerIdMap()
    {
        var map = new Dictionary<string, UINormalRankCell>();
        foreach (var kv in mSlotCellByRank)
        {
            var cell = kv.Value;
            if (cell == null || cell == mMyCell)
                continue;

            var playerId = cell.GetPlayerId();
            if (!string.IsNullOrEmpty(playerId))
                map[playerId] = cell;
        }

        return map;
    }

    // 把 MyRankyCellNode 定位到 content 中指定名次槽位。
    void PositionMyCellAtRank(int rank, Dictionary<int, Vector2> slotPos = null)
    {
        if (myRankyCellNode == null || rank <= 0)
            return;

        var my = myRankyCellNode;
        if (my.parent != content)
            my.SetParent(content, false);

        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;
        if (templateRt != null)
        {
            my.anchorMin = templateRt.anchorMin;
            my.anchorMax = templateRt.anchorMax;
            my.pivot = templateRt.pivot;
            my.sizeDelta = templateRt.sizeDelta;
        }

        if (slotPos != null && slotPos.TryGetValue(rank, out var pos))
            my.anchoredPosition = pos;
        else
        {
            var slot = FindRankCellNode($"RankyCellNode ({rank})") as RectTransform;
            if (slot != null)
                my.anchoredPosition = slot.anchoredPosition;
            else
                my.anchoredPosition = GetSlotAnchoredPos(rank);
        }

        my.localScale = Vector3.one;
        my.SetAsLastSibling();
        my.SetActiveEx(true);
    }

    void FitScrollContentHeight()
    {
        if (content == null || mRankData == null || mOriNormalCellObj == null)
            return;

        int slotCount = mRankData.Count;
        ResetRankSlotAnchorLayout(slotCount);
        var scrollRect = scrollView != null ? scrollView.GetComponent<ScrollRect>() : null;
        var contentRt = scrollRect != null && scrollRect.content != null ? scrollRect.content : content;
        GoldTreeRankScrollLayout.FitScrollContentHeight(
            contentRt,
            FindRankCellNode,
            slotCount,
            mOriNormalCellObj.GetComponent<RectTransform>(),
            scrollRect,
            includeTop3InScroll: true,
            toggleSlotVisibility: false);
    }

    static float GetCanonicalFirstRankSlotY(RectTransform templateRt)
    {
        if (templateRt == null)
            return 0f;

        // 第一名行顶贴齐 content 顶（content 锚点在上方，向下为负）。
        return -templateRt.sizeDelta.y * templateRt.pivot.y;
    }

    // 升榜动画会移动挂在 RankyCellNode 上的格子，导致槽位锚点漂移；用固定首行 Y 重算全部槽位。
    void ResetRankSlotAnchorLayout(int slotCount = -1)
    {
        if (content == null)
            return;

        if (slotCount <= 0)
            slotCount = mRankData != null ? mRankData.Count : mAllCellList.Count;
        if (slotCount <= 0)
            return;

        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;
        var firstSlot = FindRankCellNode("RankyCellNode (1)") as RectTransform;
        if (firstSlot != null && templateRt != null)
        {
            if (float.IsNaN(mCanonicalFirstRankSlotY))
                mCanonicalFirstRankSlotY = GetCanonicalFirstRankSlotY(templateRt);

            var pos = firstSlot.anchoredPosition;
            pos.x = templateRt.anchoredPosition.x;
            pos.y = mCanonicalFirstRankSlotY;
            firstSlot.anchoredPosition = pos;
        }

        var dividers = GoldTreeLeagueRankFlagHelper.GetDividerRanks();
        GoldTreeRankScrollLayout.ApplyFullRankSlotPositions(
            FindRankCellNode,
            slotCount,
            content,
            dividers);
    }

    Vector2 GetDstAnchoredPos(int dstRank)
    {
        CacheScrollRefs();
        if (mScrollRect == null || mScrollViewport == null)
            return Vector2.zero;

        var contentRt = mScrollRect.content;
        float viewportHeight = mScrollViewport.rect.height;

        RectTransform cellRt = null;
        if (dstRank > 0 && dstRank <= mAllCellList.Count)
            cellRt = mAllCellList[dstRank - 1].GetRectTransform();
        if (cellRt == null)
            cellRt = FindRankCellNode($"RankyCellNode ({dstRank})") as RectTransform;

        if (cellRt != null)
        {
            Canvas.ForceUpdateCanvases();
            return GoldTreeRankScrollLayout.GetContentPosToAlignChildCenter(contentRt, cellRt, mScrollViewport);
        }

        var firstRow = FindRankCellNode("RankyCellNode (1)") as RectTransform;
        if (firstRow == null)
        {
            var h2 = mOriNormalCellObj.GetComponent<RectTransform>().sizeDelta.y;
            float fallbackY = (dstRank - 1) * h2 - viewportHeight * 0.5f;
            return new Vector2(contentRt.anchoredPosition.x, ClampContentScrollY(fallbackY, viewportHeight));
        }

        float firstRowCenterFromTop = GoldTreeRankScrollLayout.GetRectCenterOffsetFromTop(firstRow, contentRt);
        const float defaultRowSpacing = 141f;
        float rowSpacing = defaultRowSpacing;
        var secondRow = FindRankCellNode("RankyCellNode (2)") as RectTransform;
        if (secondRow != null)
        {
            float secondRowCenterFromTop = GoldTreeRankScrollLayout.GetRectCenterOffsetFromTop(secondRow, contentRt);
            rowSpacing = Mathf.Abs(firstRowCenterFromTop - secondRowCenterFromTop);
        }

        float cellCenterFromTop = firstRowCenterFromTop + (dstRank - 1) * rowSpacing;
        float contentY = cellCenterFromTop - GoldTreeRankScrollLayout.GetViewportCenterOffsetFromTop(mScrollViewport);
        return new Vector2(contentRt.anchoredPosition.x, ClampContentScrollY(contentY, viewportHeight));
    }

    float ClampContentScrollY(float contentY, float viewportHeight)
    {
        var contentRt = mScrollRect.content;
        float maxY = Mathf.Max(0f, contentRt.rect.height - viewportHeight);
        return Mathf.Clamp(contentY, 0f, maxY);
    }

    void ScrollToRankStatic(int dstRank)
    {
        CacheScrollRefs();
        if (mScrollRect == null)
            return;

        if (dstRank == 1)
        {
            var pos = mScrollRect.content.anchoredPosition;
            pos.y = 0f;
            mScrollRect.content.anchoredPosition = pos;
            Canvas.ForceUpdateCanvases();
            return;
        }

        mScrollRect.content.anchoredPosition = GetDstAnchoredPos(dstRank);
        Canvas.ForceUpdateCanvases();
    }

    void ScrollToMyNode()
    {
        if (myRank <= 0)
            return;

        ScrollToRankStatic(myRank);
    }


    private Transform mScrollPTran;
    private GameObject mOriNormalCellObj;

    void InitListView()
    {
        var oriObj = rankyCellNode.gameObject;
        mScrollPTran = oriObj.transform.parent;
        mOriNormalCellObj = oriObj;
        oriObj.SetActive(false);
    }

    // Prefab Content 挂了 VerticalLayoutGroup；槽位 RankyCellNode(1..n) 也是按布局组
    //（锚点 0,0 + 宽度 0）准备的。必须先关掉布局组，再用模板 RankyCellNode 把所有槽位
    // 整流成与 MyRankyCellNode 一致的手动锚点尺寸，否则其它 item 宽度为 0，只剩玩家条像钉在正中。
    void DisableContentAutoLayout()
    {
        if (content == null)
            return;

        var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        var sizeFitter = content.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
            sizeFitter.enabled = false;

        NormalizeRankSlotTransforms();
    }

    // 用模板 RankyCellNode（已正确配置自动布局无关的锚点/宽度）覆盖各编号槽位的锚点与尺寸。
    // 仅影响 UIRankChangeDlg；UIRankView 仍走 GoldTreeRankScrollLayout 原逻辑。
    void NormalizeRankSlotTransforms()
    {
        var templateRt = mOriNormalCellObj != null
            ? mOriNormalCellObj.GetComponent<RectTransform>()
            : rankyCellNode;
        if (templateRt == null || content == null)
            return;

        for (int i = 1; i <= GoldTreeRankScrollLayout.MaxFullRankSlots; i++)
        {
            var slot = FindRankCellNode($"RankyCellNode ({i})") as RectTransform;
            if (slot == null)
                continue;

            slot.anchorMin = templateRt.anchorMin;
            slot.anchorMax = templateRt.anchorMax;
            slot.pivot = templateRt.pivot;
            slot.sizeDelta = templateRt.sizeDelta;

            if (i == 1)
            {
                var pos = slot.anchoredPosition;
                pos.x = templateRt.anchoredPosition.x;
                pos.y = GetCanonicalFirstRankSlotY(templateRt);
                slot.anchoredPosition = pos;
                mCanonicalFirstRankSlotY = pos.y;
            }
        }
    }

    // 取得（或惰性创建）指定名次槽位 RankyCellNode(rank) 的普通格子；每个槽位复用同一个 widget。
    UINormalRankCell GetOrCreateSlotCell(int rank)
    {
        if (mSlotCellByRank.TryGetValue(rank, out var cached) && cached != null)
        {
            cached.GetRectTransform().gameObject.SetActiveEx(true);
            return cached;
        }

        var objTran = EnsureRankSlotNode(rank);
        var cell = CreateWidget<UINormalRankCell>("slotCell" + rank, objTran.GetComponent<UIWidgetBehaviour>());
        objTran.gameObject.SetActiveEx(true);
        mSlotCellByRank[rank] = cell;
        return cell;
    }

    // 确保指定名次的 RankyCellNode 存在并已按全榜布局定位（即使该名次由 MyRankyCellNode 占用也需创建，用于高度与锚点）。
    RectTransform EnsureRankSlotNode(int rank)
    {
        return GoldTreeRankScrollLayout.EnsureNormalCell(
            rank,
            mScrollPTran,
            mOriNormalCellObj,
            FindRankCellNode,
            includeTop3InScroll: true,
            totalRankCount: mRankData != null ? mRankData.Count : rank) as RectTransform;
    }

    Transform FindRankCellNode(string cellName)
    {
        if (content == null)
            return null;

        var direct = content.Find(cellName);
        if (direct != null)
            return direct;

        foreach (Transform child in content.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == cellName)
                return child;
        }

        return null;
    }

    void HideBoxTips()
    {
        if (boxTips != null)
            boxTips.Hide();
    }

    void OnCellAwardClick(IRankCell cell)
    {
        if (boxTips == null)
            return;

        boxTips.Show(cell, mAllCellList);
    }

    TextMeshProUGUI mTitleLb;

    void CacheTitleTextRef()
    {
        if (mTitleLb != null || safeArea == null)
            return;
        GoldTreeRankTitleTextHelper.TryBindRankTipTitle(safeArea, out mTitleLb);
    }

    TextMeshProUGUI mLeagueTitleLb;
    Transform mDogRoot;
    Image mMiddleDogImage;
    SkeletonGraphic mRankDogSpine;

    void CacheLeagueTitleRef()
    {
        if (mLeagueTitleLb != null || safeArea == null)
            return;

        var titleTran = safeArea.Find("titleLb");
        if (titleTran != null)
            mLeagueTitleLb = titleTran.GetComponent<TextMeshProUGUI>();
    }

    void RefreshLeagueTitleText()
    {
        CacheLeagueTitleRef();
        if (mLeagueTitleLb == null)
            return;

        LocalizedTMPRefresher.SetText(
            mLeagueTitleLb,
            GoldTreeRobotRankRewardHelper.GetPlayerLeagueOpenTitle);
    }

    void CacheDogDisplayRefs()
    {
        if (rectTransform == null)
            return;

        mDogRoot ??= FindDeep(rectTransform, "dogSwitch") ?? FindDeep(rectTransform, "dog");
        mRankDogSpine ??= FindDeep(rectTransform, "SkeletonGraphic (RankDog)")?.GetComponent<SkeletonGraphic>();
        if (mDogRoot == null)
            return;

        mMiddleDogImage ??= ResolveDogSlotImage(mDogRoot, "middle");
    }

    static Transform FindDeep(Transform root, string nodeName)
    {
        if (root == null || string.IsNullOrEmpty(nodeName))
            return null;

        if (root.name == nodeName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), nodeName);
            if (found != null)
                return found;
        }

        return null;
    }

    static Image ResolveDogSlotImage(Transform dogRoot, string slotName)
    {
        if (dogRoot == null || string.IsNullOrEmpty(slotName))
            return null;

        var slot = dogRoot.Find(slotName);
        if (slot == null)
            return null;

        var dogImage = slot.Find("dog")?.GetComponent<Image>();
        return dogImage != null ? dogImage : slot.GetComponent<Image>();
    }

    void RefreshDogTierDisplay(bool playRankDogEnter)
    {
        CacheDogDisplayRefs();
        int currentTier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(
            GPUserDataManager.GPUserData.goldenTreeLeagueTier);
        ApplyDogSlot(mMiddleDogImage, currentTier);
        ApplyRankDog(mRankDogSpine, currentTier, playRankDogEnter);
    }

    void ApplyDogSlot(Image image, int tierId)
    {
        if (image == null)
            return;

        if (!GoldTreeRobotRankRewardHelper.IsValidTierId(tierId))
        {
            image.gameObject.SetActiveEx(false);
            return;
        }

        var sprite = LoadAtlasSprite(
            "Rankboard",
            GoldTreeRankTierDogDisplay.GetDogSpriteName(tierId));
        if (sprite == null)
        {
            image.gameObject.SetActiveEx(false);
            return;
        }

        image.sprite = sprite;
        image.gameObject.SetActiveEx(true);
    }

    static void ApplyRankDog(SkeletonGraphic spine, int tierId, bool playEnter)
    {
        if (spine == null)
            return;

        if (!GoldTreeRobotRankRewardHelper.IsValidTierId(tierId))
        {
            spine.gameObject.SetActiveEx(false);
            return;
        }

        spine.gameObject.SetActiveEx(true);
        GoldTreeRankTierDogDisplay.ApplyRankDogSkeleton(spine, tierId);

        var skeleton = spine.Skeleton;
        var state = spine.AnimationState;
        if (skeleton == null || state == null)
            return;

        if (!playEnter)
        {
            state.ClearTrack(0);
            var idleEntry = state.SetAnimation(0, "RankDog_Idle", true);
            if (idleEntry != null)
            {
                idleEntry.MixDuration = 0f;
                idleEntry.Reverse = false;
                idleEntry.TimeScale = 1f;
            }

            state.Apply(skeleton);
            spine.Update(0f);
            return;
        }

        state.ClearTrack(0);
        var inEntry = state.SetAnimation(0, "RankDog_In", false);
        if (inEntry != null)
        {
            inEntry.MixDuration = 0f;
            inEntry.Reverse = false;
            inEntry.TimeScale = 1f;
        }

        var loopEntry = state.AddAnimation(0, "RankDog_Idle", true, 0f);
        if (loopEntry != null)
            loopEntry.MixDuration = 0f;

        state.Apply(skeleton);
        spine.Update(0f);
    }

    void RefreshTitleText()
    {
        CacheTitleTextRef();
        GoldTreeRankTitleTextHelper.RefreshRankTipTitle(mTitleLb, mRankData, mCurRankData, mLastRankData);
    }

    void DelayIfRankSeq(int seq, float duration, Action action)
    {
        Delay(duration, () =>
        {
            if (seq != mRankAnimSeq)
                return;

            action?.Invoke();
        });
    }

    // 动画播放中点全屏遮罩：停掉未完成的位移，直接落到最终名次并露出 continueBtn。
    void SkipRankAnimToResult()
    {
        if (!mRankAnimPlaying || !mRankAnimSkippable)
            return;

        mRankAnimSeq++;
        mMyCell?.CancelAddTreeAnim();
        mMyCell?.CancelTop3JpPop();
        KillAllRankAnimTweens();
        CacheScrollRefs();
        if (mScrollRect != null)
        {
            mScrollRect.StopMovement();
            mScrollRect.enabled = true;
        }

        if (rankTreeCollect != null)
        {
            rankTreeCollect.Find("goldtreeNum")?.GetComponent<CanvasGroup>()?.DOKill();
            rankTreeCollect.SetActiveEx(false);
        }

        RestoreAllRankSlotsActive();
        mForceRankLayout = true;
        FinishRankAnim();
    }

    void RestoreAllRankSlotsActive()
    {
        ClearVacatedRankPlaceholder();
        int count = mRankData != null ? mRankData.Count : mAllCellList.Count;
        for (int rank = 1; rank <= count; rank++)
            ShowRankSlotNode(rank);

        foreach (var kv in mSlotCellByRank)
        {
            if (kv.Value != null && kv.Value != mMyCell)
                kv.Value.GetRectTransform().gameObject.SetActiveEx(true);
        }
    }

    void EnsureTouchCoverSkip()
    {
        if (touchCover == null || mTouchCoverBtn != null)
            return;

        mTouchCoverBtn = touchCover.GetComponent<Button>();
        if (mTouchCoverBtn == null)
        {
            mTouchCoverBtn = touchCover.gameObject.AddComponent<Button>();
            mTouchCoverBtn.transition = Selectable.Transition.None;
            mTouchCoverBtn.targetGraphic = touchCover.GetComponent<Graphic>();
        }

        var nav = mTouchCoverBtn.navigation;
        nav.mode = Navigation.Mode.None;
        mTouchCoverBtn.navigation = nav;
        mTouchCoverBtn.onClick.AddListener(SkipRankAnimToResult);
    }

    bool mTouchEnable = false;
    void SetTouchEnable(bool b)
    {
        mTouchEnable = b;
        if (touchCover != null)
            touchCover.SetActiveEx(!b);
        if (!b)
            SetContinueVisible(false);
    }

    void SetContinueVisible(bool visible)
    {
        var go = continueBtn != null ? continueBtn.gameObject : null;
        Transform named = rectTransform != null ? rectTransform.Find("SafeArea/continueBtn") : null;
        if (go == null && named != null)
            go = named.gameObject;
        if (named != null && go != null && named.gameObject != go)
            named.gameObject.SetActive(visible);
        if (go == null)
            return;

        if (!visible)
        {
            mContinuePop?.Kill();
            mContinuePop = null;
            continueBtn?.CancelPressTween(true);
            go.transform.localScale = Vector3.one;
            go.SetActive(false);
            return;
        }

        bool showPop = !go.activeSelf;
        go.SetActive(true);
        if (showPop)
            PlayContinuePop(go);
    }

    // 与 UIPanel 弹窗打开同一套：0 -> 1.05 -> 1。
    void PlayContinuePop(GameObject go)
    {
        continueBtn?.CancelPressTween();
        var rt = go.transform;
        rt.DOKill();
        rt.localScale = new Vector3(0f, 0f, 1f);
        mContinuePop?.Kill();
        mContinuePop = DOTween.Sequence()
            .Append(rt.DOScale(new Vector3(1.05f, 1.05f, 1f), 0.18f))
            .Append(rt.DOScale(Vector3.one, 0.06f))
            .SetUpdate(true)
            .SetLink(go, LinkBehaviour.KillOnDestroy);
    }

    protected override bool OnEscButtonPressed()
    {
        return IsDialogShowing();
    }
}