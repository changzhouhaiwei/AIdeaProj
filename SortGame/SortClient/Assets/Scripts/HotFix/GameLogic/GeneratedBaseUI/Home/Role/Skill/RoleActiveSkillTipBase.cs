using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleActiveSkillTipBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Skill/RoleActiveSkillTip.prefab";
    
	protected Image iconImage;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI skillDesc;

    protected override void OnBindCompsAndEvents() 
    {
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		skillDesc = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		iconImage = null;
		nameText = null;
		skillDesc = null;
	}
}