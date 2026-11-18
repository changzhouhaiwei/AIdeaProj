using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class VIPCellObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/VIP/VIPCellObj.prefab";
    
	protected TextMeshProUGUI vIPText;
	protected RectTransform vIPGold;
	protected TextMeshProUGUI vIPGoldText;
	protected RectTransform vIPExp;
	protected TextMeshProUGUI vIPExpText;
	protected RectTransform vIPTime;
	protected TextMeshProUGUI vIPTimeText;
	protected RectTransform vIPStrength;
	protected TextMeshProUGUI vIPStrengthText;
	protected RectTransform vIPDaily;
	protected TextMeshProUGUI vIPDailyText;
	protected RectTransform vIPShop;
	protected TextMeshProUGUI vIPShopText;
	protected RectTransform itemRect;
	protected Image buyIm;
	protected TextMeshProUGUI buyNum;
	protected TButton buyButton;
	protected Image buyNowIm;
	protected TextMeshProUGUI buyNowNum;
	protected TextMeshProUGUI alreadyText;

    protected override void OnBindCompsAndEvents() 
    {
		vIPText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		vIPGold = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		vIPGoldText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		vIPExp = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		vIPExpText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		vIPTime = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
		vIPTimeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		vIPStrength = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);
		vIPStrengthText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		vIPDaily = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);
		vIPDailyText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(10, 0);
		vIPShop = viewBehaviour.GetComponentByIndexs<RectTransform>(11, 0);
		vIPShopText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(12, 0);
		itemRect = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);
		buyIm = viewBehaviour.GetComponentByIndexs<Image>(14, 0);
		buyNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(15, 0);
		buyButton = viewBehaviour.GetComponentByIndexs<TButton>(16, 0);
		buyNowIm = viewBehaviour.GetComponentByIndexs<Image>(17, 0);
		buyNowNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(18, 0);
		alreadyText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(19, 0);

		BindEvent(buyButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(buyButton);

		vIPText = null;
		vIPGold = null;
		vIPGoldText = null;
		vIPExp = null;
		vIPExpText = null;
		vIPTime = null;
		vIPTimeText = null;
		vIPStrength = null;
		vIPStrengthText = null;
		vIPDaily = null;
		vIPDailyText = null;
		vIPShop = null;
		vIPShopText = null;
		itemRect = null;
		buyIm = null;
		buyNum = null;
		buyButton = null;
		buyNowIm = null;
		buyNowNum = null;
		alreadyText = null;
	}
}