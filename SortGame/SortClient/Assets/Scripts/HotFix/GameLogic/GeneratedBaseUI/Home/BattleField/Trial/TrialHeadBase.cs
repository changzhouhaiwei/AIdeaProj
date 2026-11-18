using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TrialHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/Trial/TrialHead.prefab";
    
	protected RectTransform lockRect;
	protected RectTransform itemLock;
	protected RectTransform completed;
	protected RectTransform itemCompleted;
	protected Button fightBtn;
	protected RectTransform fight;
	protected TextMeshProUGUI index;
	protected RectTransform itemFight;

    protected override void OnBindCompsAndEvents() 
    {
		lockRect = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		itemLock = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		completed = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		itemCompleted = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		fightBtn = viewBehaviour.GetComponentByIndexs<Button>(4, 0);
		fight = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
		index = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		itemFight = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);

		BindEvent(fightBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(fightBtn);

		lockRect = null;
		itemLock = null;
		completed = null;
		itemCompleted = null;
		fightBtn = null;
		fight = null;
		index = null;
		itemFight = null;
	}
}