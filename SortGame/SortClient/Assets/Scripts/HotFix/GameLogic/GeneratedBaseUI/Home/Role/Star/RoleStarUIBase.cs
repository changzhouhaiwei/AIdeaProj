using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleStarUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Star/RoleStarUI.prefab";
    
	protected UIWidgetBehaviour rolePassiveSkillHead_1;
	protected UIWidgetBehaviour rolePassiveSkillHead_2;
	protected UIWidgetBehaviour rolePassiveSkillHead_3;
	protected UIWidgetBehaviour rolePassiveSkillHead_4;
	protected TextMeshProUGUI starText;
	protected RectTransform composeBg;
	protected TextMeshProUGUI formComposeText;
	protected TextMeshProUGUI toComposeText;
	protected RectTransform composeMaxBg;
	protected RectTransform nineBg;
	protected RectTransform fromStarBg;
	protected RectTransform toStarBg;
	protected RectTransform starInfoBg;
	protected UIWidgetBehaviour roleHead_1;
	protected UIWidgetBehaviour roleStarCostHead_1;
	protected TButton starButton;
	protected RectTransform maxImage;

    protected override void OnBindCompsAndEvents() 
    {
		rolePassiveSkillHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		rolePassiveSkillHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		rolePassiveSkillHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		rolePassiveSkillHead_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		starText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		composeBg = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
		formComposeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		toComposeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		composeMaxBg = viewBehaviour.GetComponentByIndexs<RectTransform>(8, 0);
		nineBg = viewBehaviour.GetComponentByIndexs<RectTransform>(9, 0);
		fromStarBg = viewBehaviour.GetComponentByIndexs<RectTransform>(10, 0);
		toStarBg = viewBehaviour.GetComponentByIndexs<RectTransform>(11, 0);
		starInfoBg = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		roleHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(13, 0);
		roleStarCostHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(14, 0);
		starButton = viewBehaviour.GetComponentByIndexs<TButton>(15, 0);
		maxImage = viewBehaviour.GetComponentByIndexs<RectTransform>(16, 0);

		BindEvent(starButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(starButton);

		rolePassiveSkillHead_1 = null;
		rolePassiveSkillHead_2 = null;
		rolePassiveSkillHead_3 = null;
		rolePassiveSkillHead_4 = null;
		starText = null;
		composeBg = null;
		formComposeText = null;
		toComposeText = null;
		composeMaxBg = null;
		nineBg = null;
		fromStarBg = null;
		toStarBg = null;
		starInfoBg = null;
		roleHead_1 = null;
		roleStarCostHead_1 = null;
		starButton = null;
		maxImage = null;
	}
}