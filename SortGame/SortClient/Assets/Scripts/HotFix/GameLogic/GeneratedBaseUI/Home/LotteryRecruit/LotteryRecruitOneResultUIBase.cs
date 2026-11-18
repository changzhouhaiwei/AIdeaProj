using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitOneResultUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitOneResultUI.prefab";
    
	protected UIWidgetBehaviour lotteryRecruitRoleHeadObj;
	protected TButton backButton;
	protected TButton againButton;
	protected Image oneCostImage;
	protected TextMeshProUGUI oneCostText;

    protected override void OnBindCompsAndEvents() 
    {
		lotteryRecruitRoleHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		backButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		againButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		oneCostImage = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		oneCostText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);

		BindEvent(backButton);
		BindEvent(againButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(backButton);
		UnbindEvent(againButton);

		lotteryRecruitRoleHeadObj = null;
		backButton = null;
		againButton = null;
		oneCostImage = null;
		oneCostText = null;
	}
}