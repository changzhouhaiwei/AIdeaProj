using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RolePassiveSkillTipBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Star/RolePassiveSkillTip.prefab";
    
	protected Image iconImage;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI skillDesc;
	protected RectTransform layout_2;
	protected RectTransform buffBg;
	protected TextMeshProUGUI buffName;
	protected TextMeshProUGUI buffDesc;

    protected override void OnBindCompsAndEvents() 
    {
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		skillDesc = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		layout_2 = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		buffBg = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		buffName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		buffDesc = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		iconImage = null;
		nameText = null;
		skillDesc = null;
		layout_2 = null;
		buffBg = null;
		buffName = null;
		buffDesc = null;
	}
}