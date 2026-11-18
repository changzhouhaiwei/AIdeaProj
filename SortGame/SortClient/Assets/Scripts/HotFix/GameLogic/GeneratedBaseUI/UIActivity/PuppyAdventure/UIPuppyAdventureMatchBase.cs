using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIPuppyAdventureMatchBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/PuppyAdventure/UIPuppyAdventureMatch.prefab";
    	protected TextMeshProUGUI findTex;	protected TextMeshProUGUI playerNumTex;	protected TextMeshProUGUI prizeNumTex;	protected RectTransform avatars;
    protected override void OnBindCompsAndEvents() 
    {		findTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);		playerNumTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);		prizeNumTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);		avatars = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);	}

    protected override void OnUnbindCompsAndEvents() 
    {		findTex = null;		playerNumTex = null;		prizeNumTex = null;		avatars = null;	}
}