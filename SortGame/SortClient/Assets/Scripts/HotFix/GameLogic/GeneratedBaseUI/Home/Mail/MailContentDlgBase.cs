using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class MailContentDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Mail/MailContentDlg.prefab";
    
	protected TextMeshProUGUI contentLb;
	protected TextMeshProUGUI titleLb;
	protected TButton awardBtn;
	protected TextMeshProUGUI timeLb;
	protected TButton closeBtn;
	protected LoopListView2 loopListView;
	protected Image awardNode;

    protected override void OnBindCompsAndEvents() 
    {
		contentLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		titleLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		awardBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		timeLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(5, 0);
		awardNode = viewBehaviour.GetComponentByIndexs<Image>(6, 0);

		BindEvent(awardBtn);
		BindEvent(closeBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardBtn);
		UnbindEvent(closeBtn);

		contentLb = null;
		titleLb = null;
		awardBtn = null;
		timeLb = null;
		closeBtn = null;
		loopListView = null;
		awardNode = null;
	}
}