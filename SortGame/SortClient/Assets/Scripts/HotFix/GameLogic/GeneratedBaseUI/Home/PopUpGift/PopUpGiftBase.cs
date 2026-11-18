using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class PopUpGiftBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/PopUpGift/PopUpGift.prefab";
    
	protected RectTransform rewards;
	protected TButton awardButton;
	protected TextMeshProUGUI awardText;
	protected TextMeshProUGUI name;
	protected TextMeshProUGUI time;
	protected RectTransform timeObj;
	protected Button btnClose;

    protected override void OnBindCompsAndEvents() 
    {
		rewards = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		awardText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		name = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		timeObj = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
		btnClose = viewBehaviour.GetComponentByIndexs<Button>(6, 0);

		BindEvent(awardButton);
		BindEvent(btnClose);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardButton);
		UnbindEvent(btnClose);

		rewards = null;
		awardButton = null;
		awardText = null;
		name = null;
		time = null;
		timeObj = null;
		btnClose = null;
	}
}