using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ExchangeShopCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/WorldBoss/ExchangeShopCell.prefab";
    
	protected UIWidgetBehaviour exchangeShopHead;
	protected UIWidgetBehaviour exchangeShopHead1;
	protected UIWidgetBehaviour exchangeShopHead2;

    protected override void OnBindCompsAndEvents() 
    {
		exchangeShopHead = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		exchangeShopHead1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		exchangeShopHead2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		exchangeShopHead = null;
		exchangeShopHead1 = null;
		exchangeShopHead2 = null;
	}
}