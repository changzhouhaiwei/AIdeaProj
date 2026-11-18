using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RenameDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Player/RenameDlg.prefab";
    
	protected TMP_InputField inputField;
	protected TButton renameButton;
	protected Image itemIcon;
	protected TextMeshProUGUI itemText;
	protected TButton cancelButton;
	protected TButton closeButton;
	protected TextMeshProUGUI needText;

    protected override void OnBindCompsAndEvents() 
    {
		inputField = viewBehaviour.GetComponentByIndexs<TMP_InputField>(0, 0);
		renameButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		itemIcon = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		itemText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		cancelButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		needText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);

		BindEvent(inputField);
		BindEvent(renameButton);
		BindEvent(cancelButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(inputField);
		UnbindEvent(renameButton);
		UnbindEvent(cancelButton);
		UnbindEvent(closeButton);

		inputField = null;
		renameButton = null;
		itemIcon = null;
		itemText = null;
		cancelButton = null;
		closeButton = null;
		needText = null;
	}
}