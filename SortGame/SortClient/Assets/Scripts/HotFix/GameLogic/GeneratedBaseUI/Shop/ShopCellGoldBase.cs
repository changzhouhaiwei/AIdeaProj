using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopCellGoldBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/ShopCellGold.prefab";
    
	protected TextMeshProUGUI nameText;
	protected TButton buyBtn;
	protected Image icon;
	protected TextMeshProUGUI priceText;
	protected TextMeshProUGUI disNumLb;
	protected RectTransform disacountNode;

    protected override void OnBindCompsAndEvents() 
    {
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		buyBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		icon = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		priceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		disNumLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		disacountNode = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);

		BindEvent(buyBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(buyBtn);

		nameText = null;
		buyBtn = null;
		icon = null;
		priceText = null;
		disNumLb = null;
		disacountNode = null;
	}
}