using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIGoldTreeStartBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTree/UIGoldTreeStart.prefab";
    	protected TButton backBtn;	protected TButton startBtn;	protected TextMeshProUGUI boardCnt;	protected TButton closeBtn;
    protected override void OnBindCompsAndEvents() 
    {		backBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		startBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);		boardCnt = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);		closeBtn = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);		BindEvent(backBtn);		BindEvent(startBtn);		BindEvent(closeBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(backBtn);		UnbindEvent(startBtn);		UnbindEvent(closeBtn);		backBtn = null;		startBtn = null;		boardCnt = null;		closeBtn = null;	}
}