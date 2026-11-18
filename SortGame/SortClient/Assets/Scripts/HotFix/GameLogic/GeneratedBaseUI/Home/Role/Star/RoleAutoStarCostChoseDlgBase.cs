using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleAutoStarCostChoseDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Star/RoleAutoStarCostChoseDlg.prefab";
    
	protected RectTransform loopListBg;
	protected LoopListView2 loopListView;
	protected RectTransform enemyBg;
	protected RectTransform lineImage;
	protected TButton sureButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		loopListBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(1, 0);
		enemyBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		lineImage = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);

		BindEvent(sureButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sureButton);
		UnbindEvent(closeButton);

		loopListBg = null;
		loopListView = null;
		enemyBg = null;
		lineImage = null;
		sureButton = null;
		closeButton = null;
	}
}