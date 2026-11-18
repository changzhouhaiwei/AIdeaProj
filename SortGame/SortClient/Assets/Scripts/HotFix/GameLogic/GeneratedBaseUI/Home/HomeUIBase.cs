using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class HomeUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/HomeUI.prefab";
    
	protected LoopListView2 loopListView;
	protected RectTransform bottomBg;
	protected RectTransform safeAreaBg;
	protected UIWidgetBehaviour userAssetsUI;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		bottomBg = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		safeAreaBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		userAssetsUI = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		loopListView = null;
		bottomBg = null;
		safeAreaBg = null;
		userAssetsUI = null;
	}
}