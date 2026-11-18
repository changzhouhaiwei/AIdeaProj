using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleAutoStarCostChoseCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Star/RoleAutoStarCostChoseCell.prefab";
    
	protected UIWidgetBehaviour roleHead_0;
	protected UIWidgetBehaviour roleHead_1;
	protected UIWidgetBehaviour roleHead_2;
	protected UIWidgetBehaviour roleHead_3;
	protected UIWidgetBehaviour roleHead_4;

    protected override void OnBindCompsAndEvents() 
    {
		roleHead_0 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		roleHead_1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		roleHead_2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		roleHead_3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		roleHead_4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		roleHead_0 = null;
		roleHead_1 = null;
		roleHead_2 = null;
		roleHead_3 = null;
		roleHead_4 = null;
	}
}