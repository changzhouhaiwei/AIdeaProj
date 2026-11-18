using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipEmptyHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipEmptyHead.prefab";
    
	protected RectTransform animNode;
	protected TButton headButton;

    protected override void OnBindCompsAndEvents() 
    {
		animNode = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		headButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);

		BindEvent(headButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton);

		animNode = null;
		headButton = null;
	}
}