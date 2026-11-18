using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopCellSpreadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/ShopCellSpread.prefab";
    
	protected TButton spreadBtn;

    protected override void OnBindCompsAndEvents() 
    {
		spreadBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);

		BindEvent(spreadBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(spreadBtn);

		spreadBtn = null;
	}
}