using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipStarDescBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipStarDesc.prefab";
    
	protected TextMeshProUGUI starText;
	protected TextMeshProUGUI descText;
	protected TextMeshProUGUI criticalText;
	protected TextMeshProUGUI unlockText;
	protected RectTransform lineImage;

    protected override void OnBindCompsAndEvents() 
    {
		starText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		criticalText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		unlockText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		lineImage = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		starText = null;
		descText = null;
		criticalText = null;
		unlockText = null;
		lineImage = null;
	}
}