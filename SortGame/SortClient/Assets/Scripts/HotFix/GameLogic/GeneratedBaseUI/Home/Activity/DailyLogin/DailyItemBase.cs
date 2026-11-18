using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyItemBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/DailyLogin/DailyItem.prefab";
    
	protected UIWidgetBehaviour itemHead;
	protected Image dailyItemNow;
	protected TextMeshProUGUI day;
	protected TButton btnReward;

    protected override void OnBindCompsAndEvents() 
    {
		itemHead = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		dailyItemNow = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		day = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		btnReward = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);

		BindEvent(btnReward);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnReward);

		itemHead = null;
		dailyItemNow = null;
		day = null;
		btnReward = null;
	}
}