using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CumulativeRechargeUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/CumulativeRecharge/CumulativeRechargeUIObj.prefab";
    
	protected TextMeshProUGUI time;
	protected LoopListView2 loopListView;
	protected UIWidgetBehaviour cumulativeRechargeCell;

    protected override void OnBindCompsAndEvents() 
    {
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(1, 0);
		cumulativeRechargeCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		time = null;
		loopListView = null;
		cumulativeRechargeCell = null;
	}
}