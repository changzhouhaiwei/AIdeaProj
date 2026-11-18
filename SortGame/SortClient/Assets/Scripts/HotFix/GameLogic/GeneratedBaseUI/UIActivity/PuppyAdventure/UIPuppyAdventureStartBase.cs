using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIPuppyAdventureStartBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/PuppyAdventure/UIPuppyAdventureStart.prefab";
    	protected TButton startBtn;	protected TButton startGreyBtn;	protected TButton closeBtn;	protected TextMeshProUGUI titleTex;	protected TextMeshProUGUI desTex;	protected TextMeshProUGUI prizeNumTex;	protected TextMeshProUGUI prizeLabelTex;	protected TextMeshProUGUI cooldownTex;
    protected override void OnBindCompsAndEvents() 
    {		startBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		startGreyBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);		titleTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);		desTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);		prizeNumTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);		prizeLabelTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);		cooldownTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);		BindEvent(startBtn);		BindEvent(startGreyBtn);		BindEvent(closeBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(startBtn);		UnbindEvent(startGreyBtn);		UnbindEvent(closeBtn);		startBtn = null;		startGreyBtn = null;		closeBtn = null;		titleTex = null;		desTex = null;		prizeNumTex = null;		prizeLabelTex = null;		cooldownTex = null;	}
}