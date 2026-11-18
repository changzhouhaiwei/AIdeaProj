using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopRechargeUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Recharge/ShopRechargeUI.prefab";
    
	protected LoopListView2 loopListView;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		loopListView = null;
	}
}