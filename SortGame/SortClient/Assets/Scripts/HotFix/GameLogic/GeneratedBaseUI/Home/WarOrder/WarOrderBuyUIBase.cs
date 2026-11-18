using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class WarOrderBuyUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/WarOrder/WarOrderBuyUI.prefab";
    
	protected TButton buyBtn;
	protected TButton closeBtn;
	protected TextMeshProUGUI moneyLb;

    protected override void OnBindCompsAndEvents() 
    {
		buyBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		moneyLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);

		BindEvent(buyBtn);
		BindEvent(closeBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(buyBtn);
		UnbindEvent(closeBtn);

		buyBtn = null;
		closeBtn = null;
		moneyLb = null;
	}
}