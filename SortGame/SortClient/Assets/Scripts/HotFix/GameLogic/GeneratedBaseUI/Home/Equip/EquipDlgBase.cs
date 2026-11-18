using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipDlg.prefab";
    
	protected UIWidgetBehaviour equipHeadObj;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI numText;
	protected TextMeshProUGUI strengthText;
	protected TextMeshProUGUI intelligenceText;
	protected TextMeshProUGUI constitutionText;
	protected LayoutElement additionalBg;
	protected TextMeshProUGUI attackSpeedText;
	protected VerticalLayoutGroup starBg;
	protected UIWidgetBehaviour equipStarDesc_1;
	protected UIWidgetBehaviour equipStarDesc_2;
	protected UIWidgetBehaviour equipStarDesc_3;
	protected UIWidgetBehaviour equipStarDesc_4;
	protected UIWidgetBehaviour equipStarDesc_5;
	protected LayoutElement buttonBg;
	protected TButton dischargeButton;
	protected TButton cultivateButton;
	protected TButton repaceButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		equipHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		numText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		strengthText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		intelligenceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		constitutionText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		additionalBg = viewBehaviour.GetComponentByIndexs<LayoutElement>(6, 0);
		attackSpeedText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		starBg = viewBehaviour.GetComponentByIndexs<VerticalLayoutGroup>(8, 0);
		equipStarDesc_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(9, 0);
		equipStarDesc_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(10, 0);
		equipStarDesc_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(11, 0);
		equipStarDesc_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(12, 0);
		equipStarDesc_5 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(13, 0);
		buttonBg = viewBehaviour.GetComponentByIndexs<LayoutElement>(14, 0);
		dischargeButton = viewBehaviour.GetComponentByIndexs<TButton>(15, 0);
		cultivateButton = viewBehaviour.GetComponentByIndexs<TButton>(16, 0);
		repaceButton = viewBehaviour.GetComponentByIndexs<TButton>(17, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(18, 0);

		BindEvent(dischargeButton);
		BindEvent(cultivateButton);
		BindEvent(repaceButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(dischargeButton);
		UnbindEvent(cultivateButton);
		UnbindEvent(repaceButton);
		UnbindEvent(closeButton);

		equipHeadObj = null;
		nameText = null;
		numText = null;
		strengthText = null;
		intelligenceText = null;
		constitutionText = null;
		additionalBg = null;
		attackSpeedText = null;
		starBg = null;
		equipStarDesc_1 = null;
		equipStarDesc_2 = null;
		equipStarDesc_3 = null;
		equipStarDesc_4 = null;
		equipStarDesc_5 = null;
		buttonBg = null;
		dischargeButton = null;
		cultivateButton = null;
		repaceButton = null;
		closeButton = null;
	}
}