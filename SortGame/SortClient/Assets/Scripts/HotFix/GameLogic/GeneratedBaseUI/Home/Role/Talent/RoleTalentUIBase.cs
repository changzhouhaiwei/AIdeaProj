using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleTalentUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Talent/RoleTalentUI.prefab";
    
	protected UIWidgetBehaviour talentSkillHeadObj;
	protected TextMeshProUGUI desText;

    protected override void OnBindCompsAndEvents() 
    {
		talentSkillHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		desText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		talentSkillHeadObj = null;
		desText = null;
	}
}