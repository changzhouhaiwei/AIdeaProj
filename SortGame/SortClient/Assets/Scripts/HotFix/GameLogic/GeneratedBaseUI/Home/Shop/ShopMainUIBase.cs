using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopMainUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/ShopMainUI.prefab";
    
	protected TToggle toggle1;
	protected TToggle toggle3;
	protected TToggle toggle4;
	protected TToggle toggle2;
	protected UIWidgetBehaviour shopRechargeUIObj;
	protected UIWidgetBehaviour shopDailyUIObj;
	protected UIWidgetBehaviour shopExchangeUIObj;
	protected UIWidgetBehaviour giftPackUIObj;

    protected override void OnBindCompsAndEvents() 
    {
		toggle1 = viewBehaviour.GetComponentByIndexs<TToggle>(0, 0);
		toggle3 = viewBehaviour.GetComponentByIndexs<TToggle>(1, 0);
		toggle4 = viewBehaviour.GetComponentByIndexs<TToggle>(2, 0);
		toggle2 = viewBehaviour.GetComponentByIndexs<TToggle>(3, 0);
		shopRechargeUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
		shopDailyUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
		shopExchangeUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(6, 0);
		giftPackUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(7, 0);

		BindEvent(toggle1);
		BindEvent(toggle3);
		BindEvent(toggle4);
		BindEvent(toggle2);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(toggle1);
		UnbindEvent(toggle3);
		UnbindEvent(toggle4);
		UnbindEvent(toggle2);

		toggle1 = null;
		toggle3 = null;
		toggle4 = null;
		toggle2 = null;
		shopRechargeUIObj = null;
		shopDailyUIObj = null;
		shopExchangeUIObj = null;
		giftPackUIObj = null;
	}
}