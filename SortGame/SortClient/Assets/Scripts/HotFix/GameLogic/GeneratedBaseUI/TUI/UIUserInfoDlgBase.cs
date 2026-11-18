using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIUserInfoDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIMain/UIUserInfoDlg.prefab";
    
	protected Button closeButton;
	protected TButton okBtn;
	protected RectTransform headNode;
	protected RectTransform myHeadNode;
	protected TMP_InputField nameInput;
	protected TextMeshProUGUI iDText;
	protected TButton copyIdBtn;
	protected RectTransform frameNode;
	protected TToggle avatarToggle;
	protected TToggle frameToggle;

    protected override void OnBindCompsAndEvents() 
    {
		closeButton = viewBehaviour.GetComponentByIndexs<Button>(0, 0);
		okBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		headNode = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		myHeadNode = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		nameInput = viewBehaviour.GetComponentByIndexs<TMP_InputField>(4, 0);
		iDText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		copyIdBtn = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		frameNode = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);
		avatarToggle = viewBehaviour.GetComponentByIndexs<TToggle>(8, 0);
		frameToggle = viewBehaviour.GetComponentByIndexs<TToggle>(9, 0);

		BindEvent(closeButton);
		BindEvent(okBtn);
		BindEvent(nameInput);
		BindEvent(copyIdBtn);
		BindEvent(avatarToggle);
		BindEvent(frameToggle);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);
		UnbindEvent(okBtn);
		UnbindEvent(nameInput);
		UnbindEvent(copyIdBtn);
		UnbindEvent(avatarToggle);
		UnbindEvent(frameToggle);

		closeButton = null;
		okBtn = null;
		headNode = null;
		myHeadNode = null;
		nameInput = null;
		iDText = null;
		copyIdBtn = null;
		frameNode = null;
		avatarToggle = null;
		frameToggle = null;
	}
}
