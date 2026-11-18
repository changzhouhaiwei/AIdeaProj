using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleBreakthroughDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Skill/RoleBreakthroughDlg.prefab";
    
	protected RectTransform rootBg;
	protected RectTransform skillBg;
	protected Image skillImage;
	protected TextMeshProUGUI skillDescText;
	protected UIWidgetBehaviour itemHead_1;
	protected UIWidgetBehaviour itemHead_2;
	protected UIWidgetBehaviour itemHead_3;
	protected TButton breakButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		rootBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		skillBg = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		skillImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		skillDescText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		itemHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
		itemHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
		itemHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(6, 0);
		breakButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);

		BindEvent(breakButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(breakButton);
		UnbindEvent(closeButton);

		rootBg = null;
		skillBg = null;
		skillImage = null;
		skillDescText = null;
		itemHead_1 = null;
		itemHead_2 = null;
		itemHead_3 = null;
		breakButton = null;
		closeButton = null;
	}
}