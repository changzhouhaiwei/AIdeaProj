using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIPuppyAdventureRewardBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/PuppyAdventure/UIPuppyAdventureReward.prefab";
    	protected TButton tapBtn;	protected TButton closeBtn;	protected TextMeshProUGUI titleTex;	protected TextMeshProUGUI coinNumTex;	protected TextMeshProUGUI shareTex;	protected TextMeshProUGUI nextTex;	protected RectTransform coinGlow;	protected RectTransform chest;	protected RectTransform coin;
    protected override void OnBindCompsAndEvents() 
    {		tapBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);		titleTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);		coinNumTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);		shareTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);		nextTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);		coinGlow = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);		chest = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 0);		coin = viewBehaviour.GetComponentByIndexs<RectTransform>(8, 0);		BindEvent(tapBtn);		BindEvent(closeBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(tapBtn);		UnbindEvent(closeBtn);		tapBtn = null;		closeBtn = null;		titleTex = null;		coinNumTex = null;		shareTex = null;		nextTex = null;		coinGlow = null;		chest = null;		coin = null;	}
}