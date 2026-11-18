using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class GrowthFundUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/GrowthFund/GrowthFundUIObj.prefab";
    
	protected LoopListView2 loopListView;
	protected UIWidgetBehaviour growthFundCell;
	protected TButton btnAdvanced;
	protected TButton btnEnjoy;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		growthFundCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		btnAdvanced = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		btnEnjoy = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);

		BindEvent(btnAdvanced);
		BindEvent(btnEnjoy);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnAdvanced);
		UnbindEvent(btnEnjoy);

		loopListView = null;
		growthFundCell = null;
		btnAdvanced = null;
		btnEnjoy = null;
	}
}