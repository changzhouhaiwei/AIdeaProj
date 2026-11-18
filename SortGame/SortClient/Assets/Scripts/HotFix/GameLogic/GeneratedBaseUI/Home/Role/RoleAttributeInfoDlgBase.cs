using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleAttributeInfoDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleAttributeInfoDlg.prefab";
    
	protected TextMeshProUGUI attackNameText;
	protected TextMeshProUGUI attackText;
	protected TextMeshProUGUI hpText;
	protected TextMeshProUGUI attackSpeedText;
	protected TextMeshProUGUI attackRangText;
	protected Image attackRangImage;
	protected RectTransform propBg;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		attackNameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		attackText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		hpText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		attackSpeedText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		attackRangText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		attackRangImage = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		propBg = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		attackNameText = null;
		attackText = null;
		hpText = null;
		attackSpeedText = null;
		attackRangText = null;
		attackRangImage = null;
		propBg = null;
		closeButton = null;
	}
}