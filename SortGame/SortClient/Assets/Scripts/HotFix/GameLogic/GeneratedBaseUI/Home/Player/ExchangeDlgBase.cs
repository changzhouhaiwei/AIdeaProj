using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ExchangeDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Player/ExchangeDlg.prefab";
    
	protected TMP_InputField inputField;
	protected TButton canButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		inputField = viewBehaviour.GetComponentByIndexs<TMP_InputField>(0, 0);
		canButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(inputField);
		BindEvent(canButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(inputField);
		UnbindEvent(canButton);
		UnbindEvent(closeButton);

		inputField = null;
		canButton = null;
		closeButton = null;
	}
}