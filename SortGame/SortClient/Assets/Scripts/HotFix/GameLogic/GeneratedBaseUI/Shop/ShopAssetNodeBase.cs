using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopAssetNodeBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/ShopAssetNode.prefab";
    
	protected RectTransform cell1;

    protected override void OnBindCompsAndEvents() 
    {
		cell1 = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		cell1 = null;
	}
}