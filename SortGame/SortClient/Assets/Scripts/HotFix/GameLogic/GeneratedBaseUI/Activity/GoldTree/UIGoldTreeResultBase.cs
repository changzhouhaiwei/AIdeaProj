using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIGoldTreeResultBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTree/UIGoldTreeResult.prefab";
    	protected TButton claimBtn;	protected RectTransform rankAwardCell1;	protected RectTransform rankAwardCell2;	protected RectTransform rankAwardCell3;	protected RectTransform rankAwardCell4;	protected TButton backBtn;	protected TextMeshProUGUI rankLb;	protected Animator gameObject_Animator;	protected RectTransform gameObject_RectTransform;
    protected override void OnBindCompsAndEvents() 
    {		claimBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		rankAwardCell1 = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);		rankAwardCell2 = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);		rankAwardCell3 = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);		rankAwardCell4 = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);		backBtn = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);		rankLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);		gameObject_Animator = viewBehaviour.GetComponentByIndexs<Animator>(7, 0);		gameObject_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(7, 1);		BindEvent(claimBtn);		BindEvent(backBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(claimBtn);		UnbindEvent(backBtn);		claimBtn = null;		rankAwardCell1 = null;		rankAwardCell2 = null;		rankAwardCell3 = null;		rankAwardCell4 = null;		backBtn = null;		rankLb = null;		gameObject_Animator = null;		gameObject_RectTransform = null;	}
}