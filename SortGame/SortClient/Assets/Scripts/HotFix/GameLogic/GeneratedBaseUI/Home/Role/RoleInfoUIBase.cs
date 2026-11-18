using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleInfoUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleInfoUI.prefab";
    
	protected TextMeshProUGUI roleLevelText;
	protected TextMeshProUGUI attackText;
	protected TextMeshProUGUI hpText;
	protected TextMeshProUGUI attackSpeedText;
	protected Image attackRangImage;
	protected TextMeshProUGUI attackRangText;
	protected TButton infoButton;
	protected UIWidgetBehaviour activeSkillHeadObj;
	protected UIWidgetBehaviour skillHead_1;
	protected UIWidgetBehaviour skillHead_2;
	protected UIWidgetBehaviour skillHead_3;
	protected UIWidgetBehaviour skillHead_4;
	protected RectTransform expBg;
	protected Image expImage;
	protected TextMeshProUGUI expCostText;
	protected RectTransform goldBg;
	protected Image goldImage;
	protected TextMeshProUGUI goldCostText;
	protected TButton removeButton;
	protected TButton upgradeButton;
	protected TextMeshProUGUI upgradeText;
	protected TButton maxButton;
	protected TButton breakButton;
	protected TButton equipButton;

    protected override void OnBindCompsAndEvents() 
    {
		roleLevelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		attackText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		hpText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		attackSpeedText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		attackRangImage = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		attackRangText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		infoButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		activeSkillHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(7, 0);
		skillHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(8, 0);
		skillHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(9, 0);
		skillHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(10, 0);
		skillHead_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(11, 0);
		expBg = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		expImage = viewBehaviour.GetComponentByIndexs<Image>(13, 0);
		expCostText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(14, 0);
		goldBg = viewBehaviour.GetComponentByIndexs<RectTransform>(15, 0);
		goldImage = viewBehaviour.GetComponentByIndexs<Image>(16, 0);
		goldCostText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(17, 0);
		removeButton = viewBehaviour.GetComponentByIndexs<TButton>(18, 0);
		upgradeButton = viewBehaviour.GetComponentByIndexs<TButton>(19, 0);
		upgradeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(20, 0);
		maxButton = viewBehaviour.GetComponentByIndexs<TButton>(21, 0);
		breakButton = viewBehaviour.GetComponentByIndexs<TButton>(22, 0);
		equipButton = viewBehaviour.GetComponentByIndexs<TButton>(23, 0);

		BindEvent(infoButton);
		BindEvent(removeButton);
		BindEvent(upgradeButton);
		BindEvent(maxButton);
		BindEvent(breakButton);
		BindEvent(equipButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(infoButton);
		UnbindEvent(removeButton);
		UnbindEvent(upgradeButton);
		UnbindEvent(maxButton);
		UnbindEvent(breakButton);
		UnbindEvent(equipButton);

		roleLevelText = null;
		attackText = null;
		hpText = null;
		attackSpeedText = null;
		attackRangImage = null;
		attackRangText = null;
		infoButton = null;
		activeSkillHeadObj = null;
		skillHead_1 = null;
		skillHead_2 = null;
		skillHead_3 = null;
		skillHead_4 = null;
		expBg = null;
		expImage = null;
		expCostText = null;
		goldBg = null;
		goldImage = null;
		goldCostText = null;
		removeButton = null;
		upgradeButton = null;
		upgradeText = null;
		maxButton = null;
		breakButton = null;
		equipButton = null;
	}
}