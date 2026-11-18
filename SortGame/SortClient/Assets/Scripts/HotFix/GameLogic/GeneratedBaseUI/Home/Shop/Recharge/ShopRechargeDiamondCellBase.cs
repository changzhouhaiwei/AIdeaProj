using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopRechargeDiamondCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Recharge/ShopRechargeDiamondCell.prefab";
    
	protected UIWidgetBehaviour shopDiamondHead1;
	protected UIWidgetBehaviour shopDiamondHead2;

    protected override void OnBindCompsAndEvents() 
    {
		shopDiamondHead1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		shopDiamondHead2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		shopDiamondHead1 = null;
		shopDiamondHead2 = null;
	}
}