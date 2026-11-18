using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RankUIObjBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Rank/RankUIObj.prefab";
    
	protected TButton closeButton;
	protected TextMeshProUGUI content;
	protected LoopListView2 scrollView;
	protected TextMeshProUGUI oneselfIndex;
	protected TextMeshProUGUI oneselfName;
	protected TButton rankBtn;
	protected TextMeshProUGUI rankText;
	protected RectTransform rect_RectTransform;
	protected ToggleGroup rect_ToggleGroup;
	protected UIWidgetBehaviour rankToggle;
	protected RectTransform oneselfRect;
	protected RectTransform meData;

    protected override void OnBindCompsAndEvents() 
    {
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		content = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		scrollView = viewBehaviour.GetComponentByIndexs<LoopListView2>(2, 0);
		oneselfIndex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		oneselfName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		rankBtn = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		rankText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		rect_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);
		rect_ToggleGroup = viewBehaviour.GetComponentByIndexs<ToggleGroup>(7, 1);
		rankToggle = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(8, 0);
		oneselfRect = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);
		meData = viewBehaviour.GetComponentByIndexs<RectTransform>(10, 0);

		BindEvent(closeButton);
		BindEvent(rankBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);
		UnbindEvent(rankBtn);

		closeButton = null;
		content = null;
		scrollView = null;
		oneselfIndex = null;
		oneselfName = null;
		rankBtn = null;
		rankText = null;
		rect_RectTransform = null;
		rect_ToggleGroup = null;
		rankToggle = null;
		oneselfRect = null;
		meData = null;
	}
}