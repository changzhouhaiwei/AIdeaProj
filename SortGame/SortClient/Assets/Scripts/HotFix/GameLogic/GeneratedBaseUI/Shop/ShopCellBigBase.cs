using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopCellBigBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIShop/ShopCellBig.prefab";
    
	protected Image cellBg1;
	protected Image cellBg2;
	protected Image cellBg3;
	protected Image cellBg4;
	protected Image icon1;
	protected Image icon2;
	protected Image icon3;
	protected Image icon4;
	protected ContentSizeFitter conNode;
	protected TextMeshProUGUI nameText;
	protected TButton buyBtn;
	protected TextMeshProUGUI priceText;
	protected RectTransform oriAwardCell;
	protected RectTransform disacountNode;
	protected TextMeshProUGUI disNumLb;

    protected override void OnBindCompsAndEvents() 
    {
		cellBg1 = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		cellBg2 = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		cellBg3 = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		cellBg4 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		icon1 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		icon2 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		icon3 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		icon4 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		conNode = viewBehaviour.GetComponentByIndexs<ContentSizeFitter>(8, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(9, 0);
		buyBtn = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
		priceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(11, 0);
		oriAwardCell = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		disacountNode = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);
		disNumLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(14, 0);

		BindEvent(buyBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(buyBtn);

		cellBg1 = null;
		cellBg2 = null;
		cellBg3 = null;
		cellBg4 = null;
		icon1 = null;
		icon2 = null;
		icon3 = null;
		icon4 = null;
		conNode = null;
		nameText = null;
		buyBtn = null;
		priceText = null;
		oriAwardCell = null;
		disacountNode = null;
		disNumLb = null;
	}
}