using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ExchangeRankDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/WorldBoss/ExchangeRankDlg.prefab";
    
	protected LoopListView2 scrollView;
	protected TextMeshProUGUI rankNum;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		scrollView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		rankNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		scrollView = null;
		rankNum = null;
		closeButton = null;
	}
}