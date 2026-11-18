using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TalentSkillHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Talent/TalentSkillHead.prefab";
    
	protected TButton headButton;
	protected Image iconImage;
	protected TextMeshProUGUI namelText;

    protected override void OnBindCompsAndEvents() 
    {
		headButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		namelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);

		BindEvent(headButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton);

		headButton = null;
		iconImage = null;
		namelText = null;
	}
}