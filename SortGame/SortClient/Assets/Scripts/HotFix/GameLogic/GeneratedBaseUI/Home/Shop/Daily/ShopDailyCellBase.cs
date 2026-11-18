using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopDailyCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Daily/ShopDailyCell.prefab";
    
	protected UIWidgetBehaviour shopDailyHead1;
	protected UIWidgetBehaviour shopDailyHead2;
	protected UIWidgetBehaviour shopDailyHead3;

    protected override void OnBindCompsAndEvents() 
    {
		shopDailyHead1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		shopDailyHead2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		shopDailyHead3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		shopDailyHead1 = null;
		shopDailyHead2 = null;
		shopDailyHead3 = null;
	}
}