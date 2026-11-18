using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyCopyUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/DailyCopy/DailyCopyUIObj.prefab";
    
	protected TextMeshProUGUI time;
	protected LoopListView2 loopListView;
	protected Button addNum;
	protected TButton formationButton;
	protected TButton clearAllBtn;
	protected TextMeshProUGUI powerText;
	protected Image im_gold;
	protected Image im_exp;
	protected Image im_up;

    protected override void OnBindCompsAndEvents() 
    {
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(1, 0);
		addNum = viewBehaviour.GetComponentByIndexs<Button>(2, 0);
		formationButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		clearAllBtn = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		powerText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		im_gold = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		im_exp = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		im_up = viewBehaviour.GetComponentByIndexs<Image>(8, 0);

		BindEvent(addNum);
		BindEvent(formationButton);
		BindEvent(clearAllBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(addNum);
		UnbindEvent(formationButton);
		UnbindEvent(clearAllBtn);

		time = null;
		loopListView = null;
		addNum = null;
		formationButton = null;
		clearAllBtn = null;
		powerText = null;
		im_gold = null;
		im_exp = null;
		im_up = null;
	}
}