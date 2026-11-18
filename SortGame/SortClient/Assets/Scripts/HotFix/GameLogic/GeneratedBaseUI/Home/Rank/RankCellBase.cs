using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RankCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Rank/RankCell.prefab";
    
	protected RectTransform first;
	protected Image second;
	protected Image thired;
	protected TextMeshProUGUI secondName;
	protected TextMeshProUGUI thiredName;
	protected Image all;
	protected TextMeshProUGUI index;
	protected TextMeshProUGUI allName;
	protected RectTransform rect;
	protected UIWidgetBehaviour rank_EliteChallenge;
	protected UIWidgetBehaviour rank_Common;
	protected UIWidgetBehaviour rank_WorldBoss;
	protected UIWidgetBehaviour rank_Power;
	protected TextMeshProUGUI firstName;

    protected override void OnBindCompsAndEvents() 
    {
		first = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		second = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		thired = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		secondName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		thiredName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		all = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		index = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		allName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		rect = viewBehaviour.GetComponentByIndexs<RectTransform>(8, 0);
		rank_EliteChallenge = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(9, 0);
		rank_Common = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(10, 0);
		rank_WorldBoss = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(11, 0);
		rank_Power = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(12, 0);
		firstName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(13, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		first = null;
		second = null;
		thired = null;
		secondName = null;
		thiredName = null;
		all = null;
		index = null;
		allName = null;
		rect = null;
		rank_EliteChallenge = null;
		rank_Common = null;
		rank_WorldBoss = null;
		rank_Power = null;
		firstName = null;
	}
}