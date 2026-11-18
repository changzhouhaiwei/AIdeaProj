using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyBoxBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/DailyLogin/DailyBox.prefab";
    
	protected TButton btnDailyBox;
	protected Image dailyBoxOpen;
	protected Image dailyBoxFull;
	protected TextMeshProUGUI dailyBoxNum;
	protected Image dailyBox;

    protected override void OnBindCompsAndEvents() 
    {
		btnDailyBox = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		dailyBoxOpen = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		dailyBoxFull = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		dailyBoxNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		dailyBox = viewBehaviour.GetComponentByIndexs<Image>(4, 0);

		BindEvent(btnDailyBox);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnDailyBox);

		btnDailyBox = null;
		dailyBoxOpen = null;
		dailyBoxFull = null;
		dailyBoxNum = null;
		dailyBox = null;
	}
}