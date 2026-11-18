using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ShopDailyUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Shop/Daily/ShopDailyUI.prefab";
    
	protected LoopListView2 loopListView;
	protected TextMeshProUGUI timeText;
	protected RectTransform refreshCostBg;
	protected Image costImage;
	protected TextMeshProUGUI costText;
	protected TButton refreshButton;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		timeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		refreshCostBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		costImage = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		costText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		refreshButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);

		BindEvent(refreshButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(refreshButton);

		loopListView = null;
		timeText = null;
		refreshCostBg = null;
		costImage = null;
		costText = null;
		refreshButton = null;
	}
}