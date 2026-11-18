using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EliteChallengeCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/EliteChallenge/EliteChallengeCell.prefab";
    
	protected Button eliteChallengeCellBtn;
	protected Image completed;
	protected Image star1;
	protected Image star2;
	protected Image star3;
	protected TextMeshProUGUI name;
	protected Image lockCell;
	protected RectTransform move;

    protected override void OnBindCompsAndEvents() 
    {
		eliteChallengeCellBtn = viewBehaviour.GetComponentByIndexs<Button>(0, 0);
		completed = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		star1 = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		star2 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		star3 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		name = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		lockCell = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		move = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);

		BindEvent(eliteChallengeCellBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(eliteChallengeCellBtn);

		eliteChallengeCellBtn = null;
		completed = null;
		star1 = null;
		star2 = null;
		star3 = null;
		name = null;
		lockCell = null;
		move = null;
	}
}