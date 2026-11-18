using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopDailyHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Daily/ShopDailyHead.prefab";
    
	protected TButton touchButton;
	protected TextMeshProUGUI nameText;
	protected RectTransform itemBg;
	protected RectTransform discountImage;
	protected RectTransform alreadyImage;
	protected Image costImage;
	protected TextMeshProUGUI costText;

    protected override void OnBindCompsAndEvents() 
    {
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		itemBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		discountImage = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		alreadyImage = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		costImage = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		costText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);

		BindEvent(touchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);

		touchButton = null;
		nameText = null;
		itemBg = null;
		discountImage = null;
		alreadyImage = null;
		costImage = null;
		costText = null;
	}
}