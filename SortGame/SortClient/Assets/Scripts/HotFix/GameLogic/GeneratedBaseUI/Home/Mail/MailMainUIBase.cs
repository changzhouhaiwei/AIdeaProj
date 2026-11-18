using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class MailMainUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Mail/MailMainUI.prefab";
    
	protected LoopListView2 loopListView;
	protected Image noneImg;
	protected TButton closeButton;
	protected TButton lBtn;
	protected TButton rBtn;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		noneImg = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		lBtn = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		rBtn = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);

		BindEvent(closeButton);
		BindEvent(lBtn);
		BindEvent(rBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);
		UnbindEvent(lBtn);
		UnbindEvent(rBtn);

		loopListView = null;
		noneImg = null;
		closeButton = null;
		lBtn = null;
		rBtn = null;
	}
}