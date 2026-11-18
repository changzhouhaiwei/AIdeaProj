using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIApplyNotificationDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UISystemFunc/UIApplyNotificationDlg.prefab";
    
	protected TButton closeBtn;
	protected TButton yesBtn;
	protected TButton noBtn;

    protected override void OnBindCompsAndEvents() 
    {
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		yesBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		noBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(closeBtn);
		BindEvent(yesBtn);
		BindEvent(noBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeBtn);
		UnbindEvent(yesBtn);
		UnbindEvent(noBtn);

		closeBtn = null;
		yesBtn = null;
		noBtn = null;
	}
}