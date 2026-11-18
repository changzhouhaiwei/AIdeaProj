using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopFreeCoinDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/ShopFreeCoinDlg.prefab";
    
	protected TButton watchBtn;
	protected TButton closeBtn;

    protected override void OnBindCompsAndEvents() 
    {
		watchBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);

		BindEvent(watchBtn);
		BindEvent(closeBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(watchBtn);
		UnbindEvent(closeBtn);

		watchBtn = null;
		closeBtn = null;
	}
}