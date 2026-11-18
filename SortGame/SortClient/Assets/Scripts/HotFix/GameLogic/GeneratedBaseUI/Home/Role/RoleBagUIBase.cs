using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleBagUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleBagUI.prefab";
    
	protected LoopGridView loopGridView;
	protected TToggle toggle_6;
	protected TToggle toggle_0;
	protected TToggle toggle_1;
	protected TToggle toggle_2;
	protected TToggle toggle_3;
	protected TToggle toggle_4;
	protected TToggle toggle_5;
	protected TButton formationButton;
	protected TextMeshProUGUI capacityText;
	protected TButton addButton;
	protected TButton decomposeButton;
	protected TButton autoStarButton;

    protected override void OnBindCompsAndEvents() 
    {
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(0, 0);
		toggle_6 = viewBehaviour.GetComponentByIndexs<TToggle>(1, 0);
		toggle_0 = viewBehaviour.GetComponentByIndexs<TToggle>(2, 0);
		toggle_1 = viewBehaviour.GetComponentByIndexs<TToggle>(3, 0);
		toggle_2 = viewBehaviour.GetComponentByIndexs<TToggle>(4, 0);
		toggle_3 = viewBehaviour.GetComponentByIndexs<TToggle>(5, 0);
		toggle_4 = viewBehaviour.GetComponentByIndexs<TToggle>(6, 0);
		toggle_5 = viewBehaviour.GetComponentByIndexs<TToggle>(7, 0);
		formationButton = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
		capacityText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(9, 0);
		addButton = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
		decomposeButton = viewBehaviour.GetComponentByIndexs<TButton>(11, 0);
		autoStarButton = viewBehaviour.GetComponentByIndexs<TButton>(12, 0);

		BindEvent(toggle_6);
		BindEvent(toggle_0);
		BindEvent(toggle_1);
		BindEvent(toggle_2);
		BindEvent(toggle_3);
		BindEvent(toggle_4);
		BindEvent(toggle_5);
		BindEvent(formationButton);
		BindEvent(addButton);
		BindEvent(decomposeButton);
		BindEvent(autoStarButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(toggle_6);
		UnbindEvent(toggle_0);
		UnbindEvent(toggle_1);
		UnbindEvent(toggle_2);
		UnbindEvent(toggle_3);
		UnbindEvent(toggle_4);
		UnbindEvent(toggle_5);
		UnbindEvent(formationButton);
		UnbindEvent(addButton);
		UnbindEvent(decomposeButton);
		UnbindEvent(autoStarButton);

		loopGridView = null;
		toggle_6 = null;
		toggle_0 = null;
		toggle_1 = null;
		toggle_2 = null;
		toggle_3 = null;
		toggle_4 = null;
		toggle_5 = null;
		formationButton = null;
		capacityText = null;
		addButton = null;
		decomposeButton = null;
		autoStarButton = null;
	}
}