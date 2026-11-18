using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIGoldTreeLeagueEndBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTree/UIGoldTreeLeagueEnd.prefab";
    	protected TButton backBtn;	protected TextMeshProUGUI textTMP;	protected RectTransform dog;
    protected override void OnBindCompsAndEvents() 
    {		backBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		textTMP = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);		dog = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);		BindEvent(backBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(backBtn);		backBtn = null;		textTMP = null;		dog = null;	}
}