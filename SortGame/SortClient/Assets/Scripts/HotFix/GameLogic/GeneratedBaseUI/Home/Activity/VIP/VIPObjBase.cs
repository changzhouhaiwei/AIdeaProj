using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class VIPObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/VIP/VIPObj.prefab";
    
	protected TextMeshProUGUI nowVIPNum;
	protected Image slider;
	protected TextMeshProUGUI sliderNum;
	protected TButton rewardBtn;
	protected Image rewardIm;
	protected Image rewardImOpen;
	protected TButton clearButton;
	protected LoopListView2 scrollView;
	protected UIWidgetBehaviour vIPCellObj;

    protected override void OnBindCompsAndEvents() 
    {
		nowVIPNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		slider = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		sliderNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		rewardBtn = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		rewardIm = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		rewardImOpen = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		clearButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		scrollView = viewBehaviour.GetComponentByIndexs<LoopListView2>(7, 0);
		vIPCellObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(8, 0);

		BindEvent(rewardBtn);
		BindEvent(clearButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(rewardBtn);
		UnbindEvent(clearButton);

		nowVIPNum = null;
		slider = null;
		sliderNum = null;
		rewardBtn = null;
		rewardIm = null;
		rewardImOpen = null;
		clearButton = null;
		scrollView = null;
		vIPCellObj = null;
	}
}