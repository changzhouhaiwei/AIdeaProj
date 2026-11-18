using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipUpStarDescBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipUpStarDesc.prefab";
    
	protected TextMeshProUGUI starText;
	protected TextMeshProUGUI criticalText;
	protected TextMeshProUGUI unlockText;

    protected override void OnBindCompsAndEvents() 
    {
		starText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		criticalText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		unlockText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		starText = null;
		criticalText = null;
		unlockText = null;
	}
}