using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CommonAwardDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/CommonAwardDlg.prefab";
    
	protected RectTransform awardBg;
	protected LoopGridView loopGridView;

    protected override void OnBindCompsAndEvents() 
    {
		awardBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		awardBg = null;
		loopGridView = null;
	}
}