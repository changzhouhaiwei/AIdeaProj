using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopAssetCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/ShopAssetCell.prefab";
    
	protected Image icon;
	protected TextMeshProUGUI numLb;

    protected override void OnBindCompsAndEvents() 
    {
		icon = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		numLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		icon = null;
		numLb = null;
	}
}