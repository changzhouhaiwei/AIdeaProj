using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIRankViewBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTreeRank/UIRankView.prefab";
    	protected TButton helpBtn;	protected TButton okBtn;	protected RectTransform oneCell1;	protected RectTransform oneCell2;	protected RectTransform oneCell3;	protected TextMeshProUGUI timeLb;	protected TButton closeBtn;	protected RectTransform myRankyCellNode;	protected UIWidgetBehaviour rankyCellNode;	protected UIAwardTips boxTips;	protected TextMeshProUGUI boardCnt;	protected RectTransform content;	protected ScrollRect scrollView;	protected Animation uIRankView;
    protected override void OnBindCompsAndEvents() 
    {		helpBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		okBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);		oneCell1 = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);		oneCell2 = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);		oneCell3 = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);		timeLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);		myRankyCellNode = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);		rankyCellNode = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(8, 0);		boxTips = viewBehaviour.GetComponentByIndexs<UIAwardTips>(9, 0);		boardCnt = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(10, 0);		content = viewBehaviour.GetComponentByIndexs<RectTransform>(11, 0);		scrollView = viewBehaviour.GetComponentByIndexs<ScrollRect>(12, 0);		uIRankView = viewBehaviour.GetComponentByIndexs<Animation>(13, 0);		BindEvent(helpBtn);		BindEvent(okBtn);		BindEvent(closeBtn);		BindEvent(scrollView);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(helpBtn);		UnbindEvent(okBtn);		UnbindEvent(closeBtn);		UnbindEvent(scrollView);		helpBtn = null;		okBtn = null;		oneCell1 = null;		oneCell2 = null;		oneCell3 = null;		timeLb = null;		closeBtn = null;		myRankyCellNode = null;		rankyCellNode = null;		boxTips = null;		boardCnt = null;		content = null;		scrollView = null;		uIRankView = null;	}
}