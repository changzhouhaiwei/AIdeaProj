using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleAttributeRestraintDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleAttributeRestraintDlg.prefab";
    
    protected override void OnBindCompsAndEvents() 
    {
	}

    protected override void OnUnbindCompsAndEvents() 
    {
	}
}