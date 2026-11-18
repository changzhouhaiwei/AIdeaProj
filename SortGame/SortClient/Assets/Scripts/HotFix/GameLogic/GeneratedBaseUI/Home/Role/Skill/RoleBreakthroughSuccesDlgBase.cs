using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleBreakthroughSuccesDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Skill/RoleBreakthroughSuccesDlg.prefab";
    
	protected RectTransform rootBg;
	protected RectTransform skillBg;
	protected Image skillImage;
	protected TextMeshProUGUI skillDescText;

    protected override void OnBindCompsAndEvents() 
    {
		rootBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		skillBg = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		skillImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		skillDescText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		rootBg = null;
		skillBg = null;
		skillImage = null;
		skillDescText = null;
	}
}