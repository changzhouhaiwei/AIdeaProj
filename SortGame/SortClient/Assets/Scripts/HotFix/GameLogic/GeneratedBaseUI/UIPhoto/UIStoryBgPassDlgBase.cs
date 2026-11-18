using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIStoryBgPassDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIPhoto/UIStoryBgPassDlg.prefab";
    	protected Image bg;	protected TextMeshProUGUI titleText;	protected TextMeshProUGUI descLb;	protected TextMeshProUGUI levelRangeLb;	protected TButton okBtn;	protected TButton tapokBtn;
    protected override void OnBindCompsAndEvents() 
    {		bg = viewBehaviour.GetComponentByIndexs<Image>(0, 0);		titleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);		descLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);		levelRangeLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);		okBtn = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);		tapokBtn = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);		BindEvent(okBtn);		BindEvent(tapokBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(okBtn);		UnbindEvent(tapokBtn);		bg = null;		titleText = null;		descLb = null;		levelRangeLb = null;		okBtn = null;		tapokBtn = null;	}
}