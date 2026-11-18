using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopDiamondHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Recharge/ShopDiamondHead.prefab";
    
	protected TButton touchButton;
	protected TextMeshProUGUI normalText;
	protected RectTransform lineImage;
	protected TextMeshProUGUI discountText;
	protected Image diamondImage;
	protected RectTransform doubleImage;
	protected TextMeshProUGUI costText;

    protected override void OnBindCompsAndEvents() 
    {
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		normalText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		lineImage = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		discountText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		diamondImage = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		doubleImage = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
		costText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);

		BindEvent(touchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);

		touchButton = null;
		normalText = null;
		lineImage = null;
		discountText = null;
		diamondImage = null;
		doubleImage = null;
		costText = null;
	}
}