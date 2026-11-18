using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitUI.prefab";
    
	protected UIWidgetBehaviour userAssetsUIObj;
	protected Image titleImage;
	protected Image roleImage;
	protected RectTransform highLotteryBg;
	protected TButton wishButton;
	protected TButton prayButton;
	protected RectTransform plusImage;
	protected TextMeshProUGUI prayText;
	protected TButton leftButton;
	protected TButton rightButton;
	protected Image leastBg;
	protected TextMeshProUGUI highLotteryText_1;
	protected TextMeshProUGUI highLotteryText_2;
	protected TButton oneButton;
	protected RectTransform timeImage;
	protected TextMeshProUGUI timeText;
	protected UIWidgetBehaviour lotteryRecruitRotaryUIObj;
	protected Image oneCostImage;
	protected TextMeshProUGUI oneCostText;
	protected TButton freeButton;
	protected TButton tenButton;
	protected Image tenCostImage;
	protected TextMeshProUGUI tenCostText;
	protected TToggle autoDecomposeToggle;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		userAssetsUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		titleImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		roleImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		highLotteryBg = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		wishButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		prayButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		plusImage = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);
		prayText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		leftButton = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		rightButton = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);
		leastBg = viewBehaviour.GetComponentByIndexs<Image>(10, 0);
		highLotteryText_1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(11, 0);
		highLotteryText_2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(12, 0);
		oneButton = viewBehaviour.GetComponentByIndexs<TButton>(13, 0);
		timeImage = viewBehaviour.GetComponentByIndexs<RectTransform>(14, 0);
		timeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(15, 0);
		lotteryRecruitRotaryUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(16, 0);
		oneCostImage = viewBehaviour.GetComponentByIndexs<Image>(17, 0);
		oneCostText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(18, 0);
		freeButton = viewBehaviour.GetComponentByIndexs<TButton>(19, 0);
		tenButton = viewBehaviour.GetComponentByIndexs<TButton>(20, 0);
		tenCostImage = viewBehaviour.GetComponentByIndexs<Image>(21, 0);
		tenCostText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(22, 0);
		autoDecomposeToggle = viewBehaviour.GetComponentByIndexs<TToggle>(23, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(24, 0);

		BindEvent(wishButton);
		BindEvent(prayButton);
		BindEvent(leftButton);
		BindEvent(rightButton);
		BindEvent(oneButton);
		BindEvent(freeButton);
		BindEvent(tenButton);
		BindEvent(autoDecomposeToggle);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(wishButton);
		UnbindEvent(prayButton);
		UnbindEvent(leftButton);
		UnbindEvent(rightButton);
		UnbindEvent(oneButton);
		UnbindEvent(freeButton);
		UnbindEvent(tenButton);
		UnbindEvent(autoDecomposeToggle);
		UnbindEvent(closeButton);

		userAssetsUIObj = null;
		titleImage = null;
		roleImage = null;
		highLotteryBg = null;
		wishButton = null;
		prayButton = null;
		plusImage = null;
		prayText = null;
		leftButton = null;
		rightButton = null;
		leastBg = null;
		highLotteryText_1 = null;
		highLotteryText_2 = null;
		oneButton = null;
		timeImage = null;
		timeText = null;
		lotteryRecruitRotaryUIObj = null;
		oneCostImage = null;
		oneCostText = null;
		freeButton = null;
		tenButton = null;
		tenCostImage = null;
		tenCostText = null;
		autoDecomposeToggle = null;
		closeButton = null;
	}
}