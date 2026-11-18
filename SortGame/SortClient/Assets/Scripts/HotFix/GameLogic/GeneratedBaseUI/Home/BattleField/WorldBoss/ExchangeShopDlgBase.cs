using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ExchangeShopDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/WorldBoss/ExchangeShopDlg.prefab";
    
	protected LoopListView2 scrollView;
	protected UIWidgetBehaviour exchangeShopCell;
	protected TextMeshProUGUI time;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		scrollView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		exchangeShopCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		scrollView = null;
		exchangeShopCell = null;
		time = null;
		closeButton = null;
	}
}