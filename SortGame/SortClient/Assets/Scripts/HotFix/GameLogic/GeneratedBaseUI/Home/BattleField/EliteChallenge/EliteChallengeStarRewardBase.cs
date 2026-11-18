using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EliteChallengeStarRewardBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/EliteChallenge/EliteChallengeStarReward.prefab";
    
	protected RectTransform content;
	protected TextMeshProUGUI needStarNum;
	protected Button eliteChallengeStarBtn;
	protected RectTransform itemHead1;

    protected override void OnBindCompsAndEvents() 
    {
		content = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		needStarNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		eliteChallengeStarBtn = viewBehaviour.GetComponentByIndexs<Button>(2, 0);
		itemHead1 = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);

		BindEvent(eliteChallengeStarBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(eliteChallengeStarBtn);

		content = null;
		needStarNum = null;
		eliteChallengeStarBtn = null;
		itemHead1 = null;
	}
}