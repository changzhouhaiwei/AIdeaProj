using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleSkillTipBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Skill/RoleSkillTip.prefab";
    
	protected Image iconImage;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI levelText;
	protected LayoutElement skillDescBg_1;
	protected TextMeshProUGUI skillLevelText_1;
	protected TextMeshProUGUI skillDesc_1;
	protected LayoutElement skillDescBg_2;
	protected TextMeshProUGUI skillLevelText_2;
	protected TextMeshProUGUI skillDesc_2;
	protected LayoutElement skillDescBg_3;
	protected TextMeshProUGUI skillLevelText_3;
	protected TextMeshProUGUI skillDesc_3;
	protected RectTransform layout;
	protected RectTransform buffBg;
	protected TextMeshProUGUI buffName;
	protected TextMeshProUGUI buffDesc;

    protected override void OnBindCompsAndEvents() 
    {
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		levelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		skillDescBg_1 = viewBehaviour.GetComponentByIndexs<LayoutElement>(3, 0);
		skillLevelText_1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		skillDesc_1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		skillDescBg_2 = viewBehaviour.GetComponentByIndexs<LayoutElement>(6, 0);
		skillLevelText_2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		skillDesc_2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		skillDescBg_3 = viewBehaviour.GetComponentByIndexs<LayoutElement>(9, 0);
		skillLevelText_3 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(10, 0);
		skillDesc_3 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(11, 0);
		layout = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		buffBg = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);
		buffName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(14, 0);
		buffDesc = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(15, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		iconImage = null;
		nameText = null;
		levelText = null;
		skillDescBg_1 = null;
		skillLevelText_1 = null;
		skillDesc_1 = null;
		skillDescBg_2 = null;
		skillLevelText_2 = null;
		skillDesc_2 = null;
		skillDescBg_3 = null;
		skillLevelText_3 = null;
		skillDesc_3 = null;
		layout = null;
		buffBg = null;
		buffName = null;
		buffDesc = null;
	}
}