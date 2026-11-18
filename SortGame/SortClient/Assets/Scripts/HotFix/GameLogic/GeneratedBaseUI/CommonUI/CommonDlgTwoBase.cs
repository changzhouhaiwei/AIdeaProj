using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CommonDlgTwoBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/CommonDlgTwo.prefab";
    
	protected TButton cancelButton;
	protected TButton sureButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		cancelButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(cancelButton);
		BindEvent(sureButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(cancelButton);
		UnbindEvent(sureButton);
		UnbindEvent(closeButton);

		cancelButton = null;
		sureButton = null;
		closeButton = null;
	}
}