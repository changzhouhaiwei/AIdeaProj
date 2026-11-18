using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class PostTaskUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/PostTaskUI.prefab";
    
	protected LoopListView2 loopListView;
	protected RectTransform costBg;
	protected Image costImage;
	protected TextMeshProUGUI costText;
	protected TButton refreshButton;
	protected TextMeshProUGUI refreshText;
	protected TButton awardButton;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		costBg = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		costImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		costText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		refreshButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		refreshText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);

		BindEvent(refreshButton);
		BindEvent(awardButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(refreshButton);
		UnbindEvent(awardButton);

		loopListView = null;
		costBg = null;
		costImage = null;
		costText = null;
		refreshButton = null;
		refreshText = null;
		awardButton = null;
	}
}