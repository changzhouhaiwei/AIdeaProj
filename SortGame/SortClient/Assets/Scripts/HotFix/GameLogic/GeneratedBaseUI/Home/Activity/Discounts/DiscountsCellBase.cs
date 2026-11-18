using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DiscountsCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/Discounts/DiscountsCell.prefab";
    
	protected TextMeshProUGUI name;
	protected RectTransform reward;
	protected TextMeshProUGUI content;
	protected TButton btnReward;
	protected TextMeshProUGUI priceText;
	protected Image mask;

    protected override void OnBindCompsAndEvents() 
    {
		name = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		reward = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		content = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		btnReward = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		priceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		mask = viewBehaviour.GetComponentByIndexs<Image>(5, 0);

		BindEvent(btnReward);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnReward);

		name = null;
		reward = null;
		content = null;
		btnReward = null;
		priceText = null;
		mask = null;
	}
}