using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class SkillHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Skill/SkillHead.prefab";
    
	protected TButton headButton;
	protected Image iconImage;
	protected RectTransform lockImage;
	protected TextMeshProUGUI levelText;
	protected RectTransform enemyBg;

    protected override void OnBindCompsAndEvents() 
    {
		headButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		lockImage = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		levelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		enemyBg = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);

		BindEvent(headButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton);

		headButton = null;
		iconImage = null;
		lockImage = null;
		levelText = null;
		enemyBg = null;
	}
}