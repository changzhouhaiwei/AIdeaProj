using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitRuleDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitRuleDlg.prefab";
    
	protected TextMeshProUGUI descText;
	protected LoopGridView loopGridView;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(1, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		descText = null;
		loopGridView = null;
		closeButton = null;
	}
}