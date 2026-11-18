using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleUpgradeTipBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleUpgradeTip.prefab";
    
	protected RectTransform nineBg;
	protected TextMeshProUGUI tipText;

    protected override void OnBindCompsAndEvents() 
    {
		nineBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		tipText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		nineBg = null;
		tipText = null;
	}
}