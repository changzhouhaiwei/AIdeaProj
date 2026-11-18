using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopExchangeCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Exchange/ShopExchangeCell.prefab";
    
	protected UIWidgetBehaviour shopExchangeHead1;
	protected UIWidgetBehaviour shopExchangeHead2;
	protected UIWidgetBehaviour shopExchangeHead3;

    protected override void OnBindCompsAndEvents() 
    {
		shopExchangeHead1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		shopExchangeHead2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		shopExchangeHead3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		shopExchangeHead1 = null;
		shopExchangeHead2 = null;
		shopExchangeHead3 = null;
	}
}