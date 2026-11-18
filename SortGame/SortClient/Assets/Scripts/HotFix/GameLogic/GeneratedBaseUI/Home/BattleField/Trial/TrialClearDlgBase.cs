using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TrialClearDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/Trial/TrialClearDlg.prefab";
    
	protected TButton closeButton;
	protected TextMeshProUGUI num;
	protected RectTransform reward;
	protected TButton clearButton;
	protected TextMeshProUGUI count;

    protected override void OnBindCompsAndEvents() 
    {
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		num = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		reward = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		clearButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		count = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);

		BindEvent(closeButton);
		BindEvent(clearButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);
		UnbindEvent(clearButton);

		closeButton = null;
		num = null;
		reward = null;
		clearButton = null;
		count = null;
	}
}