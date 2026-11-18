using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UseBoxItemDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/ItemBag/UseBoxItemDlg.prefab";
    
	protected LoopListView2 loopListView;
	protected TextMeshProUGUI useText;
	protected TButton sureButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(0, 0);
		useText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);

		BindEvent(sureButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sureButton);
		UnbindEvent(closeButton);

		loopListView = null;
		useText = null;
		sureButton = null;
		closeButton = null;
	}
}