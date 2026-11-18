using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitTenResultUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitTenResultUI.prefab";
    
	protected UIWidgetBehaviour lotteryRecruitRoleHead_1;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_2;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_3;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_4;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_5;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_6;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_7;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_8;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_9;
	protected UIWidgetBehaviour lotteryRecruitRoleHead_10;
	protected TButton backButton;
	protected TButton againButton;
	protected Image tenCostImage;
	protected TextMeshProUGUI tenCostText;

    protected override void OnBindCompsAndEvents() 
    {
		lotteryRecruitRoleHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		lotteryRecruitRoleHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		lotteryRecruitRoleHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		lotteryRecruitRoleHead_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		lotteryRecruitRoleHead_5 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
		lotteryRecruitRoleHead_6 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
		lotteryRecruitRoleHead_7 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(6, 0);
		lotteryRecruitRoleHead_8 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(7, 0);
		lotteryRecruitRoleHead_9 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(8, 0);
		lotteryRecruitRoleHead_10 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(9, 0);
		backButton = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
		againButton = viewBehaviour.GetComponentByIndexs<TButton>(11, 0);
		tenCostImage = viewBehaviour.GetComponentByIndexs<Image>(12, 0);
		tenCostText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(13, 0);

		BindEvent(backButton);
		BindEvent(againButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(backButton);
		UnbindEvent(againButton);

		lotteryRecruitRoleHead_1 = null;
		lotteryRecruitRoleHead_2 = null;
		lotteryRecruitRoleHead_3 = null;
		lotteryRecruitRoleHead_4 = null;
		lotteryRecruitRoleHead_5 = null;
		lotteryRecruitRoleHead_6 = null;
		lotteryRecruitRoleHead_7 = null;
		lotteryRecruitRoleHead_8 = null;
		lotteryRecruitRoleHead_9 = null;
		lotteryRecruitRoleHead_10 = null;
		backButton = null;
		againButton = null;
		tenCostImage = null;
		tenCostText = null;
	}
}