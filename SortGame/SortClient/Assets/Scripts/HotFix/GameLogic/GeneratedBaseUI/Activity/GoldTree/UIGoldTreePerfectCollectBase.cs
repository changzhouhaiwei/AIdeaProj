using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIGoldTreePerfectCollectBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIActivity/GoldTree/UIGoldTreePerfectCollect.prefab";
    	protected TButton backBtn;
    protected override void OnBindCompsAndEvents() 
    {		backBtn = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);		BindEvent(backBtn);	}

    protected override void OnUnbindCompsAndEvents() 
    {		UnbindEvent(backBtn);		backBtn = null;	}
}