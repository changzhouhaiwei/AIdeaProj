using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopCellNoAdsBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/ShopCellNoAds.prefab";
    
	protected TextMeshProUGUI nameText;
	protected TButton buyBtn;
	protected TextMeshProUGUI priceText;
	protected TextMeshProUGUI descText;
	protected RectTransform disacountNode;
	protected TextMeshProUGUI disNumLb;
	protected TextMeshProUGUI coinNum;

    protected override void OnBindCompsAndEvents() 
    {
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		buyBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		priceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		disacountNode = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		disNumLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		coinNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);

		BindEvent(buyBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(buyBtn);

		nameText = null;
		buyBtn = null;
		priceText = null;
		descText = null;
		disacountNode = null;
		disNumLb = null;
		coinNum = null;
	}
}