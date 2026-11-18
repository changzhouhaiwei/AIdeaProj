using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EliteChallengeUIObjBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/EliteChallenge/EliteChallengeUIObj.prefab";
    
	protected RectTransform content;
	protected TextMeshProUGUI starNum;
	protected Image change;
	protected Button changeBtn;
	protected TextMeshProUGUI number;
	protected Button numberBtn;
	protected UIWidgetBehaviour eliteChallengeStarReward;
	protected UIWidgetBehaviour eliteChallengeCell;
	protected ScrollRect scrollViewStar;
	protected TButton closeButton;
	protected RectTransform starContent;
	protected ScrollRect scrollViewCell;
	protected TButton rankBtn;

    protected override void OnBindCompsAndEvents() 
    {
		content = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		starNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		change = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		changeBtn = viewBehaviour.GetComponentByIndexs<Button>(3, 0);
		number = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		numberBtn = viewBehaviour.GetComponentByIndexs<Button>(5, 0);
		eliteChallengeStarReward = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(6, 0);
		eliteChallengeCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(7, 0);
		scrollViewStar = viewBehaviour.GetComponentByIndexs<ScrollRect>(8, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);
		starContent = viewBehaviour.GetComponentByIndexs<RectTransform>(10, 0);
		scrollViewCell = viewBehaviour.GetComponentByIndexs<ScrollRect>(11, 0);
		rankBtn = viewBehaviour.GetComponentByIndexs<TButton>(12, 0);

		BindEvent(changeBtn);
		BindEvent(numberBtn);
		BindEvent(scrollViewStar);
		BindEvent(closeButton);
		BindEvent(scrollViewCell);
		BindEvent(rankBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(changeBtn);
		UnbindEvent(numberBtn);
		UnbindEvent(scrollViewStar);
		UnbindEvent(closeButton);
		UnbindEvent(scrollViewCell);
		UnbindEvent(rankBtn);

		content = null;
		starNum = null;
		change = null;
		changeBtn = null;
		number = null;
		numberBtn = null;
		eliteChallengeStarReward = null;
		eliteChallengeCell = null;
		scrollViewStar = null;
		closeButton = null;
		starContent = null;
		scrollViewCell = null;
		rankBtn = null;
	}
}