using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class OfflineRewardDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/OfflineReward/OfflineRewardDlg.prefab";
    
	protected TextMeshProUGUI goldText;
	protected TextMeshProUGUI expText;
	protected LoopGridView loopGridView;
	protected TextMeshProUGUI staminaText;
	protected TButton fastAwardButton;
	protected TextMeshProUGUI timeText;
	protected TButton awardButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		goldText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		expText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(2, 0);
		staminaText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		fastAwardButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		timeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);

		BindEvent(fastAwardButton);
		BindEvent(awardButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(fastAwardButton);
		UnbindEvent(awardButton);
		UnbindEvent(closeButton);

		goldText = null;
		expText = null;
		loopGridView = null;
		staminaText = null;
		fastAwardButton = null;
		timeText = null;
		awardButton = null;
		closeButton = null;
	}
}