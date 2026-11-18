using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleRebirthDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleRebirthDlg.prefab";
    
	protected TextMeshProUGUI descText;
	protected TButton sureButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(sureButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sureButton);
		UnbindEvent(closeButton);

		descText = null;
		sureButton = null;
		closeButton = null;
	}
}