using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopGoldHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Recharge/ShopGoldHead.prefab";
    
	protected TButton touchButton;
	protected TextMeshProUGUI normalText;
	protected Image costImage;
	protected TextMeshProUGUI costText;

    protected override void OnBindCompsAndEvents() 
    {
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		normalText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		costImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		costText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);

		BindEvent(touchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);

		touchButton = null;
		normalText = null;
		costImage = null;
		costText = null;
	}
}