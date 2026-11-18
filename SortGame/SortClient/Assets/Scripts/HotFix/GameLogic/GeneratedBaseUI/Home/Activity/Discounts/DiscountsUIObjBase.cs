using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DiscountsUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/Discounts/DiscountsUIObj.prefab";
    
	protected TButton btnReward;
	protected TextMeshProUGUI priceText;
	protected UIWidgetBehaviour discountsCell1;
	protected UIWidgetBehaviour discountsCell2;
	protected UIWidgetBehaviour discountsCell3;
	protected UIWidgetBehaviour discountsCell4;

    protected override void OnBindCompsAndEvents() 
    {
		btnReward = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		priceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		discountsCell1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		discountsCell2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		discountsCell3 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
		discountsCell4 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);

		BindEvent(btnReward);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnReward);

		btnReward = null;
		priceText = null;
		discountsCell1 = null;
		discountsCell2 = null;
		discountsCell3 = null;
		discountsCell4 = null;
	}
}