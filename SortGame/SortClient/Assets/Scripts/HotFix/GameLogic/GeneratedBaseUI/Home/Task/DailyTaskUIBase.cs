using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyTaskUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/DailyTaskUI.prefab";
    
	protected RectTransform activeImageBarBg;
	protected Image activeImageBar;
	protected RectTransform dailyTaskActiveBoxBg;
	protected TextMeshProUGUI activeText;
	protected LoopListView2 loopListView;
	protected TButton awardButton;

    protected override void OnBindCompsAndEvents() 
    {
		activeImageBarBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		activeImageBar = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		dailyTaskActiveBoxBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		activeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(4, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);

		BindEvent(awardButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardButton);

		activeImageBarBg = null;
		activeImageBar = null;
		dailyTaskActiveBoxBg = null;
		activeText = null;
		loopListView = null;
		awardButton = null;
	}
}