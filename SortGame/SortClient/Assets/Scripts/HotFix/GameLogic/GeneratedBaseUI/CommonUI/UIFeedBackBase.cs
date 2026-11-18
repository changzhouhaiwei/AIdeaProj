using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIFeedBackBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/UIFeedBack.prefab";
    
	protected Button closeButton;
	protected InputField inputFieldLegacy;
	protected TButton customerBtn;

    protected override void OnBindCompsAndEvents() 
    {
		closeButton = viewBehaviour.GetComponentByIndexs<Button>(0, 0);
		inputFieldLegacy = viewBehaviour.GetComponentByIndexs<InputField>(1, 0);
		customerBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(closeButton);
		BindEvent(inputFieldLegacy);
		BindEvent(customerBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);
		UnbindEvent(inputFieldLegacy);
		UnbindEvent(customerBtn);

		closeButton = null;
		inputFieldLegacy = null;
		customerBtn = null;
	}
}