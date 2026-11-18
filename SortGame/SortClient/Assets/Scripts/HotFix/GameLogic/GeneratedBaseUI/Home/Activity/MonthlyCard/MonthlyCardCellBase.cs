using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class MonthlyCardCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/MonthlyCard/MonthlyCardCell.prefab";
    
	protected TextMeshProUGUI content;
	protected Image itemIcon;
	protected TextMeshProUGUI itemNum;
	protected RectTransform reward;
	protected TextMeshProUGUI remainingDays;
	protected TButton btnBuy;
	protected TextMeshProUGUI price;
	protected TButton btnReward;
	protected RectTransform fristBuy;

    protected override void OnBindCompsAndEvents() 
    {
		content = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		itemIcon = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		itemNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		reward = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		remainingDays = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		btnBuy = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		price = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		btnReward = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		fristBuy = viewBehaviour.GetComponentByIndexs<RectTransform>(8, 0);

		BindEvent(btnBuy);
		BindEvent(btnReward);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnBuy);
		UnbindEvent(btnReward);

		content = null;
		itemIcon = null;
		itemNum = null;
		reward = null;
		remainingDays = null;
		btnBuy = null;
		price = null;
		btnReward = null;
		fristBuy = null;
	}
}