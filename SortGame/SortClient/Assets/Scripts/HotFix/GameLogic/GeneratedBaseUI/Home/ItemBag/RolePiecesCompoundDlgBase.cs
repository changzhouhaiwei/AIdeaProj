using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RolePiecesCompoundDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/ItemBag/RolePiecesCompoundDlg.prefab";
    
	protected UIWidgetBehaviour itemHead;
	protected TextMeshProUGUI nameText;
	protected TextMeshProUGUI numText;
	protected TextMeshProUGUI typeText;
	protected TMP_InputField inputField;
	protected TButton subtractButton;
	protected TButton plusButton;
	protected TButton maxButton;
	protected TButton compoundButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		itemHead = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		numText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		typeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		inputField = viewBehaviour.GetComponentByIndexs<TMP_InputField>(4, 0);
		subtractButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		plusButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		maxButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		compoundButton = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);

		BindEvent(inputField);
		BindEvent(subtractButton);
		BindEvent(plusButton);
		BindEvent(maxButton);
		BindEvent(compoundButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(inputField);
		UnbindEvent(subtractButton);
		UnbindEvent(plusButton);
		UnbindEvent(maxButton);
		UnbindEvent(compoundButton);
		UnbindEvent(closeButton);

		itemHead = null;
		nameText = null;
		numText = null;
		typeText = null;
		inputField = null;
		subtractButton = null;
		plusButton = null;
		maxButton = null;
		compoundButton = null;
		closeButton = null;
	}
}