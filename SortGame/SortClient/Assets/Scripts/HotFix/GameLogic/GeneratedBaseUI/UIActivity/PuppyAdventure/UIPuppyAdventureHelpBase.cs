using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIPuppyAdventureHelpBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/PuppyAdventure/UIPuppyAdventureHelp.prefab";
    	protected TButton closeBtn;	protected TextMeshProUGUI titleTex;	protected TextMeshProUGUI help1;	protected TextMeshProUGUI help2;	protected TextMeshProUGUI help3;	protected TextMeshProUGUI help4;
    protected override void OnBindCompsAndEvents() 
    {		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		titleTex = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);		help1 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);		help2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);		help3 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);		help4 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);		BindEvent(closeBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(closeBtn);		closeBtn = null;		titleTex = null;		help1 = null;		help2 = null;		help3 = null;		help4 = null;	}
}