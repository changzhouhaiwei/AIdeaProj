using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ActivityMainBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/ActivityMain.prefab";
    
	protected TButton closeButton;
	protected LoopListView2 loopListView;
	protected UIWidgetBehaviour activityButton;
	protected RectTransform pages;
	protected Image bg;

    protected override void OnBindCompsAndEvents() 
    {
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(1, 0);
		activityButton = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		pages = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		bg = viewBehaviour.GetComponentByIndexs<Image>(4, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		closeButton = null;
		loopListView = null;
		activityButton = null;
		pages = null;
		bg = null;
	}
}