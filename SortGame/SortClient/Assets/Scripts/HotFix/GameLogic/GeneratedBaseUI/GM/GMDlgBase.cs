using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class GMDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/GM/GMDlg.prefab";
    
	protected TButton touchButton;
	protected RectTransform nineBg;
	protected TextMeshProUGUI uidText;
	protected TMP_InputField inputItemType;
	protected TMP_InputField inputItemId;
	protected TMP_InputField inputItemCnt;
	protected TMP_InputField searchInput;
	protected TButton sureButton;
	protected LoopListView2 loopListView2;
	protected LoopListView2 loopListView1;
	protected TButton closeButton;
	protected TButton gmButton;

    protected override void OnBindCompsAndEvents() 
    {
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		nineBg = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		uidText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		inputItemType = viewBehaviour.GetComponentByIndexs<TMP_InputField>(3, 0);
		inputItemId = viewBehaviour.GetComponentByIndexs<TMP_InputField>(4, 0);
		inputItemCnt = viewBehaviour.GetComponentByIndexs<TMP_InputField>(5, 0);
		searchInput = viewBehaviour.GetComponentByIndexs<TMP_InputField>(6, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		loopListView2 = viewBehaviour.GetComponentByIndexs<LoopListView2>(8, 0);
		loopListView1 = viewBehaviour.GetComponentByIndexs<LoopListView2>(9, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
		gmButton = viewBehaviour.GetComponentByIndexs<TButton>(11, 0);

		BindEvent(touchButton);
		BindEvent(inputItemType);
		BindEvent(inputItemId);
		BindEvent(inputItemCnt);
		BindEvent(searchInput);
		BindEvent(sureButton);
		BindEvent(closeButton);
		BindEvent(gmButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);
		UnbindEvent(inputItemType);
		UnbindEvent(inputItemId);
		UnbindEvent(inputItemCnt);
		UnbindEvent(searchInput);
		UnbindEvent(sureButton);
		UnbindEvent(closeButton);
		UnbindEvent(gmButton);

		touchButton = null;
		nineBg = null;
		uidText = null;
		inputItemType = null;
		inputItemId = null;
		inputItemCnt = null;
		searchInput = null;
		sureButton = null;
		loopListView2 = null;
		loopListView1 = null;
		closeButton = null;
		gmButton = null;
	}
}