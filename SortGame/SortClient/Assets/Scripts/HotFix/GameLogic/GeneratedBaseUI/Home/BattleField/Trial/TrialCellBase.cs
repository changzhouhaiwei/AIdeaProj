using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TrialCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/Trial/TrialCell.prefab";
    
	protected Image last;
	protected UIWidgetBehaviour trialHead001;
	protected UIWidgetBehaviour trialHead002;
	protected UIWidgetBehaviour trialHead003;
	protected UIWidgetBehaviour trialHead004;
	protected Image middle;
	protected UIWidgetBehaviour trialHead01;
	protected UIWidgetBehaviour trialHead02;
	protected UIWidgetBehaviour trialHead03;
	protected Image first;
	protected UIWidgetBehaviour trialHead1;
	protected UIWidgetBehaviour trialHead2;

    protected override void OnBindCompsAndEvents() 
    {
		last = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		trialHead001 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		trialHead002 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
		trialHead003 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
		trialHead004 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
		middle = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		trialHead01 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(6, 0);
		trialHead02 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(7, 0);
		trialHead03 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(8, 0);
		first = viewBehaviour.GetComponentByIndexs<Image>(9, 0);
		trialHead1 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(10, 0);
		trialHead2 = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(11, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		last = null;
		trialHead001 = null;
		trialHead002 = null;
		trialHead003 = null;
		trialHead004 = null;
		middle = null;
		trialHead01 = null;
		trialHead02 = null;
		trialHead03 = null;
		first = null;
		trialHead1 = null;
		trialHead2 = null;
	}
}