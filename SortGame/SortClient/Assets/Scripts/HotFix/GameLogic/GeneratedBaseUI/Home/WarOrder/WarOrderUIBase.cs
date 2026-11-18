using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class WarOrderUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/WarOrder/WarOrderUI.prefab";
    
	protected TButton rewardBtn;
	protected TextMeshProUGUI timeLb;
	protected TButton unlockBtn;
	protected Image expBar;
	protected TextMeshProUGUI expLb;
	protected TButton backBtn;
	protected LoopListView2 loopListView;
	protected TextMeshProUGUI lvLb;
	protected TextMeshProUGUI expLbMax;

    protected override void OnBindCompsAndEvents() 
    {
		rewardBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		timeLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		unlockBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		expBar = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		expLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		backBtn = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(6, 0);
		lvLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		expLbMax = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);

		BindEvent(rewardBtn);
		BindEvent(unlockBtn);
		BindEvent(backBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(rewardBtn);
		UnbindEvent(unlockBtn);
		UnbindEvent(backBtn);

		rewardBtn = null;
		timeLb = null;
		unlockBtn = null;
		expBar = null;
		expLb = null;
		backBtn = null;
		loopListView = null;
		lvLb = null;
		expLbMax = null;
	}
}