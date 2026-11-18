using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleStarSuccesDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Star/RoleStarSuccesDlg.prefab";
    
	protected UIWidgetBehaviour fromRoleHead;
	protected UIWidgetBehaviour toRoleHead;
	protected RectTransform rootBg;
	protected RectTransform skillBg;
	protected Image skillImage;
	protected TextMeshProUGUI skillDescText;

    protected override void OnBindCompsAndEvents() 
    {
		fromRoleHead = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		toRoleHead = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		rootBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		skillBg = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		skillImage = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		skillDescText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		fromRoleHead = null;
		toRoleHead = null;
		rootBg = null;
		skillBg = null;
		skillImage = null;
		skillDescText = null;
	}
}