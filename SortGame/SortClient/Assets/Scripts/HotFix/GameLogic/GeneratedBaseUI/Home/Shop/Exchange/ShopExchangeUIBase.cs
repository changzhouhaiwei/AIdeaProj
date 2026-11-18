using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopExchangeUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Exchange/ShopExchangeUI.prefab";
    
	protected TToggle toggle1;
	protected TToggle toggle2;
	protected Image exchangeItemImage;
	protected TextMeshProUGUI exchangeCntText;
	protected LoopListView2 loopListView;
	protected TextMeshProUGUI timeText;

    protected override void OnBindCompsAndEvents() 
    {
		toggle1 = viewBehaviour.GetComponentByIndexs<TToggle>(0, 0);
		toggle2 = viewBehaviour.GetComponentByIndexs<TToggle>(1, 0);
		exchangeItemImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		exchangeCntText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(4, 0);
		timeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);

		BindEvent(toggle1);
		BindEvent(toggle2);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(toggle1);
		UnbindEvent(toggle2);

		toggle1 = null;
		toggle2 = null;
		exchangeItemImage = null;
		exchangeCntText = null;
		loopListView = null;
		timeText = null;
	}
}