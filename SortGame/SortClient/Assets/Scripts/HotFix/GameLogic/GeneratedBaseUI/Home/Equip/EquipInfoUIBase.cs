using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipInfoUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipInfoUI.prefab";
    
	protected TButton backButton;
	protected TToggle toggle_1;
	protected TToggle toggle_2;
	protected UIWidgetBehaviour equipUpgradeUIObj;
	protected UIWidgetBehaviour equipStartUIObj;

    protected override void OnBindCompsAndEvents() 
    {
		backButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		toggle_1 = viewBehaviour.GetComponentByIndexs<TToggle>(1, 0);
		toggle_2 = viewBehaviour.GetComponentByIndexs<TToggle>(2, 0);
		equipUpgradeUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		equipStartUIObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);

		BindEvent(backButton);
		BindEvent(toggle_1);
		BindEvent(toggle_2);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(backButton);
		UnbindEvent(toggle_1);
		UnbindEvent(toggle_2);

		backButton = null;
		toggle_1 = null;
		toggle_2 = null;
		equipUpgradeUIObj = null;
		equipStartUIObj = null;
	}
}