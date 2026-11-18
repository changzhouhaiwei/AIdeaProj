using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CardCallCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/CardCall/CardCallCell.prefab";
    
	protected TextMeshProUGUI taskDescription;
	protected Image slider;
	protected Image full;
	protected TextMeshProUGUI sliderText;
	protected RectTransform awardBg;
	protected Image received;
	protected TButton btnReward;
	protected TButton jumpButton;

    protected override void OnBindCompsAndEvents() 
    {
		taskDescription = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		slider = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		full = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		sliderText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		awardBg = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		received = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		btnReward = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		jumpButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);

		BindEvent(btnReward);
		BindEvent(jumpButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnReward);
		UnbindEvent(jumpButton);

		taskDescription = null;
		slider = null;
		full = null;
		sliderText = null;
		awardBg = null;
		received = null;
		btnReward = null;
		jumpButton = null;
	}
}