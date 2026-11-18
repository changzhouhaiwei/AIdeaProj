using System.Collections.Generic;
using UnityEngine.UI;
using FishFramework;
using UnityEngine;
using TMPro;
using TileGamePlay;

public class UIRankView : UIRankViewBase
{
    public static UIRankView Instance;
    protected override string PoolObjPath => "UIRoot/UICanvas/UILayerRoot/Layer6-Panel/UIRankView";

    List<IRankCell> mAllCellList = new();
    List<RankData.OneUserData> mRankData;
    IRankCell myCell;
    GoldTreeRankTierDogDisplay mTierDogDisplay;
    Transform mSafeArea;
    Button mBtnLeft;
    Button mBtnRight;
    TextMeshProUGUI mLeagueText;
    TextMeshProUGUI mTitleSpText;

    private bool mSeasonOver = false;
    private bool mSettlementFlowActive = false;
    private bool mSettlementClaimed = false;
    public bool IsSettlementFlowActive => mSettlementFlowActive;
    protected override void OnClicked(Button button)
    {
        if (mForbidTouch)
        {
            return;
        }

        base.OnClicked(button);
        if (button == closeBtn || button == helpBtn)
        {
            if (TileGameCore.AudioController.AudioClips != null)
                TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.buttonSound);
        }
        else if (button == mBtnLeft || button == mBtnRight)
        {
            if (TileGameCore.AudioController.AudioClips != null)
                TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.buttonSound);
        }

        if (button == closeBtn)
        {
            HideMe();
        }
        else if (button == helpBtn)
        {
            UIGameFunc.OpenGoldTreeHelp();
        }
        else if (button == mBtnLeft)
        {
            mTierDogDisplay?.TryShiftView(-1);
        }
        else if (button == mBtnRight)
        {
            mTierDogDisplay?.TryShiftView(1);
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        Instance = this;
        CacheSafeArea();
        HideBoxTips();
        HideRankTipTitle();

        InitMyCell();
        HideFixedTop3Display();
        InitTopCells();
        InitListView();
        InitTierDogDisplay();
        InitTitleSpText();

        if (GameModule.Setting.GmMode != true)
            boardCnt.SetActiveEx(false);
    }

    private bool mBHide = false;
    public void HideMe()
    {
        bool settlement = mSettlementFlowActive;
        mBHide = true;
        SetVisible(false);
        gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-3500, 0);
        StopTimer();
        if (mSettlementFlowActive && mSettlementClaimed)
        {
            GameData.Rank.OnSettlementClaimedAndClosed();
            GameEvent.Send((int)EventNameDef.GoldTreeRewardedFinish);
        }
        else
        {
            GameData.Rank.ExitSettlementBoard();
        }
        mSettlementFlowActive = false;
        mSettlementClaimed = false;
        if (settlement)
            GameData.GoldTreeActData?.NotifySettlementFlowClosed();
    }

    public void MarkSettlementClaimed()
    {
        if (!mSettlementFlowActive)
        {
            return;
        }
        mSettlementClaimed = true;
    }

    Timer countTimer;
    long mPrevCountdownRemaining = -1;
    public void ShowMe()
    {
        mSeasonOver = false;
        mBHide = false;
        SetVisible(true);
        uIRankView.Play();
        gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        // 纵向为 0 表示底部
        scrollView.verticalNormalizedPosition = 1f;

        // scrollView.scro
        StopTimer();
        mPrevCountdownRemaining = -1;
        UpdateCount(1.0f);
        countTimer = Interval(1.0f, UpdateCount);

        HideRankTipTitle();
        mRankData = GameData.Rank.GetRankList();
        RefreshData();
        boardCnt.text = GPUserDataManager.GPUserData.goldenTreeBoardId.ToString(); // GameData.Rank.GetCurrentBoardId().ToString();
        RefreshTierDogDisplay();
        RefreshTitleSpText();
    }

    public void RefreshAfterLeagueTierChanged()
    {
        RefreshTierDogDisplay();

        if (mBHide)
        {
            return;
        }

        mRankData = GameData.Rank.GetRankList();
        RefreshData();
        RefreshTitleSpText();
    }

    void RefreshTierDogDisplay()
    {
        if (mTierDogDisplay == null)
            return;

        if (mSettlementFlowActive)
        {
            var (_, newTier) = GoldTreeRobotRankRewardHelper.ResolveSettlementBoardDisplayTiers();
            mTierDogDisplay.ResetToSettlementViewTier(newTier);
        }
        else
        {
            mTierDogDisplay.ResetToPlayerTier();
        }
    }

    void StopTimer()
    {
        if (countTimer != null)
        {
            Unschedule(countTimer);
            countTimer = null;
        }
    }

    void UpdateCount(float tm)
    {
        if (mSeasonOver) return;

        var nowServerTime = GameData.GetServerTime();
        var endTime = GameData.GoldTreeActData.GetEndTime();
        var deltaTime = endTime - nowServerTime;
        long remaining = GameData.GoldTreeActData.GetCountdownRemaining();
        GameData.GoldTreeActData.TryReportActivityCountdownEnded(mPrevCountdownRemaining, remaining);
        if (remaining >= 0)
            mPrevCountdownRemaining = remaining;
        timeLb.text = FishFramework.Utility.GetHourMinSec(deltaTime);
    }


    void RefreshData()
    {
        int rank = 1;
        foreach (var item in mRankData)
        {
            UINormalRankCell oneCell;
            if (mAllCellList.Count >= rank)
            {
                oneCell = mAllCellList[rank - 1] as UINormalRankCell;
                oneCell.UpdateData(item);
            }
            else
            {
                oneCell = CreateNormalCell(rank);
                if (oneCell == null)
                    continue;
                oneCell.UpdateData(item);
            }

            // InitTopCells 预建的前 3 名复用时也要刷背景，否则玩家在前几名不会切到 cellBg2。
            oneCell.RefreshBgByMe();
            rank++;
        }

        myCell.UpdateData(GameData.Rank.GetMyRankData());
        if (myCell is UINormalRankCell myNormalCell)
            myNormalCell.SetSelfBg();
        myCell.SetBtnClickCb(OnCellAwardClick);

        foreach (var item in mAllCellList)
        {
            item.SetBtnClickCb(OnCellAwardClick);
        }

        FitScrollContentHeight();
    }

    void CacheSafeArea()
    {
        if (rectTransform == null)
            return;

        mSafeArea = rectTransform.Find("SafeArea");
        if (mSafeArea == null)
            mSafeArea = FindDeep(rectTransform, "SafeArea");
        if (mSafeArea == null && scrollView != null)
            mSafeArea = scrollView.transform.parent;
    }

    Transform GetUiRoot()
    {
        return mSafeArea != null ? mSafeArea : rectTransform;
    }

    void HideRankTipTitle()
    {
        var rankTip = FindDeep(GetUiRoot(), "titleLb (1)");
        if (rankTip != null)
            rankTip.gameObject.SetActive(false);
    }

    void FitScrollContentHeight()
    {
        if (content == null || mRankData == null || mOriNormalCellObj == null || scrollView == null)
            return;

        int slotCount = mRankData.Count;
        var contentRt = scrollView.content != null ? scrollView.content : content;
        GoldTreeRankScrollLayout.FitScrollContentHeight(
            contentRt,
            FindRankCellNode,
            slotCount,
            mOriNormalCellObj.GetComponent<RectTransform>(),
            scrollView,
            includeTop3InScroll: true);
    }

    void HideFixedTop3Display()
    {
        oneCell1.SetActiveEx(false);
        oneCell2.SetActiveEx(false);
        oneCell3.SetActiveEx(false);

        if (oneCell1 != null && oneCell1.parent != null && oneCell1.parent.name == "top3Node")
            oneCell1.parent.gameObject.SetActiveEx(false);
    }

    void InitMyCell()
    {
        var widget = ResolveRankCellWidget(myRankyCellNode);
        if (widget == null)
        {
            Debug.LogError("[UIRankView] MyRankyCellNode UIWidgetBehaviour not found.");
            return;
        }

        myCell = CreateWidget<UINormalRankCell>("myCell", widget);
        if (myCell is UINormalRankCell myNormalCell)
            myNormalCell.SetSelfBg();
    }

    static UIWidgetBehaviour ResolveRankCellWidget(Transform cellNode)
    {
        if (cellNode == null)
            return null;

        return cellNode.GetComponent<UIWidgetBehaviour>()
            ?? cellNode.GetComponentInChildren<UIWidgetBehaviour>(true);
    }

    void InitTopCells()
    {
        for (int rank = 1; rank <= 3; rank++)
        {
            var node = FindRankCellNode($"RankyCellNode ({rank})");
            if (node == null)
                continue;

            var widget = ResolveRankCellWidget(node);
            if (widget == null)
                continue;

            var cell = CreateWidget<UINormalRankCell>($"cell{rank}", widget);
            node.gameObject.SetActive(true);
            mAllCellList.Add(cell);
        }
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

    void InitTierDogDisplay()
    {
        mTierDogDisplay ??= new GoldTreeRankTierDogDisplay();

        var uiRoot = GetUiRoot();
        var btnLeftTran = FindDeep(uiRoot, "btn_left");
        var btnRightTran = FindDeep(uiRoot, "btn_right");
        mBtnLeft = btnLeftTran != null ? btnLeftTran.GetComponent<Button>() : null;
        mBtnRight = btnRightTran != null ? btnRightTran.GetComponent<Button>() : null;

        if (mBtnLeft != null)
            BindEvent(mBtnLeft);
        if (mBtnRight != null)
            BindEvent(mBtnRight);

        var leagueTextTran = FindDeep(uiRoot, "LeagueText");
        if (leagueTextTran != null)
        {
            mLeagueText = leagueTextTran.GetComponent<TextMeshProUGUI>();
            if (mLeagueText == null)
                mLeagueText = leagueTextTran.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        var dogRoot = FindDeep(uiRoot, "dogSwitch") ?? FindDeep(uiRoot, "dog");
        mTierDogDisplay.Bind(
            dogRoot != null ? dogRoot : uiRoot,
            mBtnLeft,
            mBtnRight,
            UpdateLeagueText,
            ResolveDogShiftDuration(),
            enableInteractiveLockSpine: true);
    }

    float ResolveDogShiftDuration()
    {
        var settings = viewBehaviour.GetComponent<GoldTreeRankTierDogDisplaySettings>();
        return settings != null
            ? settings.ShiftDuration
            : GoldTreeRankTierDogDisplay.DefaultShiftDuration;
    }

    void InitTitleSpText()
    {
        var uiRoot = GetUiRoot();
        var titleSpTran = FindDeep(uiRoot, "titleSp");
        if (titleSpTran == null)
            return;

        var textTran = titleSpTran.Find("Text") ?? titleSpTran.Find("text");
        mTitleSpText = textTran != null
            ? textTran.GetComponent<TextMeshProUGUI>()
            : titleSpTran.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void RefreshTitleSpText()
    {
        if (mTitleSpText == null)
            return;

        if (mSettlementFlowActive)
        {
            LocalizedTMPRefresher.SetText(mTitleSpText, () =>
            {
                var (_, newTier) = GoldTreeRobotRankRewardHelper.ResolveSettlementBoardDisplayTiers();
                return GoldTreeRobotRankRewardHelper.GetLeagueOpenTitle(newTier);
            });
            return;
        }

        LocalizedTMPRefresher.SetText(
            mTitleSpText,
            GoldTreeRobotRankRewardHelper.GetPlayerLeagueOpenTitle);
    }

    void UpdateLeagueText(int viewTierId)
    {
        if (mLeagueText == null)
            return;

        int tierId = viewTierId;
        LocalizedTMPRefresher.SetText(
            mLeagueText,
            () => GoldTreeRobotRankRewardHelper.GetTierDisplayName(tierId));
    }

    UINormalRankCell CreateNormalCell(int rank)
    {
        var objTran = GoldTreeRankScrollLayout.EnsureNormalCell(
            rank,
            mScrollPTran,
            mOriNormalCellObj,
            FindRankCellNode,
            includeTop3InScroll: true,
            totalRankCount: mRankData != null ? mRankData.Count : rank);

        var widget = ResolveRankCellWidget(objTran);
        if (widget == null)
            return null;

        var cell = CreateWidget<UINormalRankCell>("cell" + rank, widget);
        mAllCellList.Add(cell);
        objTran.gameObject.SetActive(true);
        return cell;
    }

    Transform FindRankCellNode(string cellName)
    {
        var scrollContent = GetScrollContent();
        if (scrollContent == null)
            return null;

        var direct = scrollContent.Find(cellName);
        if (direct != null)
            return direct;

        foreach (Transform child in scrollContent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == cellName)
                return child;
        }

        return null;
    }

    RectTransform GetScrollContent()
    {
        if (scrollView != null && scrollView.content != null)
            return scrollView.content;
        return content;
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

        boxTips.Show(cell, CollectTipsCells());
    }

    List<IRankCell> mTipsCells;

    List<IRankCell> CollectTipsCells()
    {
        if (mTipsCells == null)
            mTipsCells = new List<IRankCell>(mAllCellList.Count + 1);
        else
            mTipsCells.Clear();

        mTipsCells.AddRange(mAllCellList);
        if (myCell != null && !mAllCellList.Contains(myCell))
            mTipsCells.Add(myCell);
        return mTipsCells;
    }

    private bool mForbidTouch = false;
    public void ShowGetResultAward()
    {
        GameData.Rank.PrepareLastSeasonSettlementBoard();
        mSettlementFlowActive = true;
        mSettlementClaimed = false;
        ShowMe();
        mForbidTouch = true;
        timeLb.text = "Finished";
        mSeasonOver = true;

        TileGameCore.Tween.DelayedCall(1.0f, () =>
        {
            mForbidTouch = false;
        });

        TileGameCore.Tween.DelayedCall(0.5f, () =>
        {
            var node = GameModule.UI.OpenPanel<UIGoldTreeResult>();
            var myData = GameData.Rank.GetMyRankData();
            if (myData == null)
            {
                Debug.LogWarning("[UIRankView] Settlement award skipped: player rank data not found.");
                return;
            }

            cfg.Game.GoldTreeRankAwardCfg awardData =
                GameData.GoldTreeActData.GetAwardCfgByRank((int)myData.rank);
            node.ShowRank(myData.rank, myData);
            node.ShowAwards(awardData);
        });
    }

    protected override bool OnEscButtonPressed()
    {
        if (!mBHide)
        {
            HideMe();
            return true;
        }
        return false;
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        if (Instance == this)
        {
            Instance = null;
        }

        mTierDogDisplay?.Dispose();
        mTierDogDisplay = null;
        mSafeArea = null;
        mBtnLeft = null;
        mBtnRight = null;
        mLeagueText = null;
        mTitleSpText = null;
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
}