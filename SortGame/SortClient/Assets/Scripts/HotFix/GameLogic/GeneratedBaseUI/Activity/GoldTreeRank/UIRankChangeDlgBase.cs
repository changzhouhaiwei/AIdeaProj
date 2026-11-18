using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIRankChangeDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTreeRank/UIRankChangeDlg.prefab";

	protected TButton helpBtn;
	protected TextMeshProUGUI timeLb;
	protected TButton closeBtn;
	protected RectTransform myRankyCellNode;
	protected RectTransform rankyCellNode;
	protected UIAwardTips boxTips;
	protected TextMeshProUGUI boardCnt;
	protected TButton continueBtn;
	protected RectTransform scrollView;
	protected RectTransform touchCover;
	protected RectTransform safeArea;
	protected RectTransform rankTreeCollect;
	protected RectTransform myRankUpEff;
	protected RectTransform myRankUpArrive;
	protected RectTransform content;

    protected override void OnBindCompsAndEvents()
    {
		helpBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		timeLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		myRankyCellNode = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		rankyCellNode = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		boxTips = viewBehaviour.GetComponentByIndexs<UIAwardTips>(5, 0);
		boardCnt = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		continueBtn = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		scrollView = viewBehaviour.GetComponentByIndexs<RectTransform>(8, 0);
		touchCover = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);
		safeArea = viewBehaviour.GetComponentByIndexs<RectTransform>(10, 0);
		rankTreeCollect = viewBehaviour.GetComponentByIndexs<RectTransform>(11, 0);
		myRankUpEff = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		myRankUpArrive = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);
		content = viewBehaviour.GetComponentByIndexs<RectTransform>(14, 0);

		BindEvent(helpBtn);
		BindEvent(closeBtn);
		BindEvent(continueBtn);
	}

    protected override void OnUnbindCompsAndEvents()
    {
		UnbindEvent(helpBtn);
		UnbindEvent(closeBtn);
		UnbindEvent(continueBtn);

		helpBtn = null;
		timeLb = null;
		closeBtn = null;
		myRankyCellNode = null;
		rankyCellNode = null;
		boxTips = null;
		boardCnt = null;
		continueBtn = null;
		scrollView = null;
		touchCover = null;
		safeArea = null;
		rankTreeCollect = null;
		myRankUpEff = null;
		myRankUpArrive = null;
		content = null;
	}
}
