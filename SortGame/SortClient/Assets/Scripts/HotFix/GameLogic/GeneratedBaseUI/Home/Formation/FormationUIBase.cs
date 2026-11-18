using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class FormationUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Formation/FormationUI.prefab";
    
	protected UIWidgetBehaviour formationRoleHead_1;
	protected UIWidgetBehaviour formationRoleHead_2;
	protected UIWidgetBehaviour formationRoleHead_3;
	protected UIWidgetBehaviour formationRoleHead_4;
	protected LoopGridView loopGridView;
	protected TButton offButton;
	protected TButton saveButton;
	protected TButton closeButton;
	protected TToggle toggle_5;
	protected TToggle toggle_6;
	protected TToggle toggle_3;
	protected TToggle toggle_0;
	protected TToggle toggle_1;
	protected TToggle toggle_4;
	protected TToggle toggle_2;

    protected override void OnBindCompsAndEvents() 
    {
		formationRoleHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		formationRoleHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		formationRoleHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		formationRoleHead_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(4, 0);
		offButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		saveButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		toggle_5 = viewBehaviour.GetComponentByIndexs<TToggle>(8, 0);
		toggle_6 = viewBehaviour.GetComponentByIndexs<TToggle>(9, 0);
		toggle_3 = viewBehaviour.GetComponentByIndexs<TToggle>(10, 0);
		toggle_0 = viewBehaviour.GetComponentByIndexs<TToggle>(11, 0);
		toggle_1 = viewBehaviour.GetComponentByIndexs<TToggle>(12, 0);
		toggle_4 = viewBehaviour.GetComponentByIndexs<TToggle>(13, 0);
		toggle_2 = viewBehaviour.GetComponentByIndexs<TToggle>(14, 0);

		BindEvent(offButton);
		BindEvent(saveButton);
		BindEvent(closeButton);
		BindEvent(toggle_5);
		BindEvent(toggle_6);
		BindEvent(toggle_3);
		BindEvent(toggle_0);
		BindEvent(toggle_1);
		BindEvent(toggle_4);
		BindEvent(toggle_2);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(offButton);
		UnbindEvent(saveButton);
		UnbindEvent(closeButton);
		UnbindEvent(toggle_5);
		UnbindEvent(toggle_6);
		UnbindEvent(toggle_3);
		UnbindEvent(toggle_0);
		UnbindEvent(toggle_1);
		UnbindEvent(toggle_4);
		UnbindEvent(toggle_2);

		formationRoleHead_1 = null;
		formationRoleHead_2 = null;
		formationRoleHead_3 = null;
		formationRoleHead_4 = null;
		loopGridView = null;
		offButton = null;
		saveButton = null;
		closeButton = null;
		toggle_5 = null;
		toggle_6 = null;
		toggle_3 = null;
		toggle_0 = null;
		toggle_1 = null;
		toggle_4 = null;
		toggle_2 = null;
	}
}