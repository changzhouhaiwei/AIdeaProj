using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class MonthlyCardUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/MonthlyCard/MonthlyCardUIObj.prefab";
    
	protected UIWidgetBehaviour monthlyCardCell;
	protected UIWidgetBehaviour premiumMonthlyCardCell;

    protected override void OnBindCompsAndEvents() 
    {
		monthlyCardCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		premiumMonthlyCardCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		monthlyCardCell = null;
		premiumMonthlyCardCell = null;
	}
}