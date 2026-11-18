using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DispatchDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/DispatchDlg.prefab";
    
	protected TextMeshProUGUI titleText;
	protected Image titleImage;
	protected RectTransform timeBg;
	protected TextMeshProUGUI timeText;
	protected UIWidgetBehaviour dispatchRoleHead_1;
	protected UIWidgetBehaviour dispatchRoleHead_2;
	protected UIWidgetBehaviour dispatchRoleHead_3;
	protected TextMeshProUGUI starText;
	protected Image sx_0;
	protected Image sx_1;
	protected Image sx_2;
	protected Image sx_3;
	protected Image sx_4;
	protected Image sx_5;
	protected LoopGridView loopGridView;
	protected TToggle toggle_5;
	protected TToggle toggle_2;
	protected TToggle toggle_6;
	protected TToggle toggle_4;
	protected TToggle toggle_3;
	protected TToggle toggle_0;
	protected TToggle toggle_1;
	protected TButton autoSelectButton;
	protected TButton dispatchButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		titleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		titleImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		timeBg = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		timeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		dispatchRoleHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
		dispatchRoleHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
		dispatchRoleHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(6, 0);
		starText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		sx_0 = viewBehaviour.GetComponentByIndexs<Image>(8, 0);
		sx_1 = viewBehaviour.GetComponentByIndexs<Image>(9, 0);
		sx_2 = viewBehaviour.GetComponentByIndexs<Image>(10, 0);
		sx_3 = viewBehaviour.GetComponentByIndexs<Image>(11, 0);
		sx_4 = viewBehaviour.GetComponentByIndexs<Image>(12, 0);
		sx_5 = viewBehaviour.GetComponentByIndexs<Image>(13, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(14, 0);
		toggle_5 = viewBehaviour.GetComponentByIndexs<TToggle>(15, 0);
		toggle_2 = viewBehaviour.GetComponentByIndexs<TToggle>(16, 0);
		toggle_6 = viewBehaviour.GetComponentByIndexs<TToggle>(17, 0);
		toggle_4 = viewBehaviour.GetComponentByIndexs<TToggle>(18, 0);
		toggle_3 = viewBehaviour.GetComponentByIndexs<TToggle>(19, 0);
		toggle_0 = viewBehaviour.GetComponentByIndexs<TToggle>(20, 0);
		toggle_1 = viewBehaviour.GetComponentByIndexs<TToggle>(21, 0);
		autoSelectButton = viewBehaviour.GetComponentByIndexs<TButton>(22, 0);
		dispatchButton = viewBehaviour.GetComponentByIndexs<TButton>(23, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(24, 0);

		BindEvent(toggle_5);
		BindEvent(toggle_2);
		BindEvent(toggle_6);
		BindEvent(toggle_4);
		BindEvent(toggle_3);
		BindEvent(toggle_0);
		BindEvent(toggle_1);
		BindEvent(autoSelectButton);
		BindEvent(dispatchButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(toggle_5);
		UnbindEvent(toggle_2);
		UnbindEvent(toggle_6);
		UnbindEvent(toggle_4);
		UnbindEvent(toggle_3);
		UnbindEvent(toggle_0);
		UnbindEvent(toggle_1);
		UnbindEvent(autoSelectButton);
		UnbindEvent(dispatchButton);
		UnbindEvent(closeButton);

		titleText = null;
		titleImage = null;
		timeBg = null;
		timeText = null;
		dispatchRoleHead_1 = null;
		dispatchRoleHead_2 = null;
		dispatchRoleHead_3 = null;
		starText = null;
		sx_0 = null;
		sx_1 = null;
		sx_2 = null;
		sx_3 = null;
		sx_4 = null;
		sx_5 = null;
		loopGridView = null;
		toggle_5 = null;
		toggle_2 = null;
		toggle_6 = null;
		toggle_4 = null;
		toggle_3 = null;
		toggle_0 = null;
		toggle_1 = null;
		autoSelectButton = null;
		dispatchButton = null;
		closeButton = null;
	}
}