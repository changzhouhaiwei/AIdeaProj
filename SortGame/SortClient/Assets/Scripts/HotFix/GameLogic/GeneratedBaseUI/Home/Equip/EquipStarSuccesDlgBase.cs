using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipStarSuccesDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipStarSuccesDlg.prefab";
    
	protected UIWidgetBehaviour equipHeadObj;

    protected override void OnBindCompsAndEvents() 
    {
		equipHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		equipHeadObj = null;
	}
}