using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIPrivacyDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UISystemFunc/UIPrivacyDlg.prefab";
    
	protected TButton okBtn;
	protected TButton privacyBtn;
	protected TButton serviceBtn;

    protected override void OnBindCompsAndEvents() 
    {
		okBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		privacyBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		serviceBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(okBtn);
		BindEvent(privacyBtn);
		BindEvent(serviceBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(okBtn);
		UnbindEvent(privacyBtn);
		UnbindEvent(serviceBtn);

		okBtn = null;
		privacyBtn = null;
		serviceBtn = null;
	}
}