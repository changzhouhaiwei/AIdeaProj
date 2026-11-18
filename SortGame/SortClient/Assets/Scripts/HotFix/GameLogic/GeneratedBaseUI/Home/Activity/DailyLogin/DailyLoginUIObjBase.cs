using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyLoginUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/DailyLogin/DailyLoginUIObj.prefab";
    
	protected UIWidgetBehaviour dailyItem;
	protected Image dailyBoxFill;
	protected TextMeshProUGUI day;
	protected RectTransform dailyReward;
	protected RectTransform dailyBoxRT;
	protected UIWidgetBehaviour dailyBox;

    protected override void OnBindCompsAndEvents() 
    {
		dailyItem = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		dailyBoxFill = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		day = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		dailyReward = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		dailyBoxRT = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		dailyBox = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		dailyItem = null;
		dailyBoxFill = null;
		day = null;
		dailyReward = null;
		dailyBoxRT = null;
		dailyBox = null;
	}
}