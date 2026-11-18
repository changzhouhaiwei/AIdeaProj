using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopRechargeGoldCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Recharge/ShopRechargeGoldCell.prefab";
    
	protected UIWidgetBehaviour shopGoldHead1;
	protected UIWidgetBehaviour shopGoldHead2;
	protected UIWidgetBehaviour shopGoldHead3;

    protected override void OnBindCompsAndEvents() 
    {
		shopGoldHead1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		shopGoldHead2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		shopGoldHead3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		shopGoldHead1 = null;
		shopGoldHead2 = null;
		shopGoldHead3 = null;
	}
}