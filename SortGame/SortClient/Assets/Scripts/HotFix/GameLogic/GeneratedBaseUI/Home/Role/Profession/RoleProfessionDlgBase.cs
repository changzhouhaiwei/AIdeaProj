using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleProfessionDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Profession/RoleProfessionDlg.prefab";
    
	protected LoopListView2 loopListView;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		loopListView = null;
		closeButton = null;
	}
}