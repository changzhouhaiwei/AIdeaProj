using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIShopMainBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/UIShopMain.prefab";
    	protected TButton backButton;	protected LoopListView2 loopListView;	protected UIWidgetBehaviour shopCellNoAds;	protected UIWidgetBehaviour shopCellGold;	protected UIWidgetBehaviour shopCellBig;	protected UIWidgetBehaviour assetNode_UIWidgetBehaviour;	protected RectTransform assetNode_RectTransform;	protected RectTransform safeArea;
    protected override void OnBindCompsAndEvents() 
    {		backButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(1, 0);		shopCellNoAds = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);		shopCellGold = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);		shopCellBig = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);		assetNode_UIWidgetBehaviour = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);		assetNode_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 1);		safeArea = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);		BindEvent(backButton);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(backButton);		backButton = null;		loopListView = null;		shopCellNoAds = null;		shopCellGold = null;		shopCellBig = null;		assetNode_UIWidgetBehaviour = null;		assetNode_RectTransform = null;		safeArea = null;	}
}