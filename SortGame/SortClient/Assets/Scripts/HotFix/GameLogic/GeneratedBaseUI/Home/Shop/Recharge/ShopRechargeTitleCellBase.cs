using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopRechargeTitleCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Recharge/ShopRechargeTitleCell.prefab";
    
	protected TextMeshProUGUI nameText;

    protected override void OnBindCompsAndEvents() 
    {
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		nameText = null;
	}
}