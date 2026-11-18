using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIDailyWinRewardDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/DailyReward/UIDailyWinRewardDlg.prefab";
    	protected Button closeButton;	protected RectTransform dailyWinReward;	protected Button startButton;	protected TextMeshProUGUI quotoText;	protected TextMeshProUGUI totalWinCount;
    protected override void OnBindCompsAndEvents() 
    {		closeButton = viewBehaviour.GetComponentByIndexs<Button>(0, 0);		dailyWinReward = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);		startButton = viewBehaviour.GetComponentByIndexs<Button>(2, 0);		quotoText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);		totalWinCount = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);		BindEvent(closeButton);		BindEvent(startButton);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(closeButton);		UnbindEvent(startButton);		closeButton = null;		dailyWinReward = null;		startButton = null;		quotoText = null;		totalWinCount = null;	}
}