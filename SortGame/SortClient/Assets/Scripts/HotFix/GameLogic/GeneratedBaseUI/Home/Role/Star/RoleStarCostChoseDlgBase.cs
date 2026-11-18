using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleStarCostChoseDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Star/RoleStarCostChoseDlg.prefab";
    
	protected TextMeshProUGUI descText;
	protected LoopGridView loopGridView;
	protected TButton cancelButton;
	protected TButton sureButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(1, 0);
		cancelButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);

		BindEvent(cancelButton);
		BindEvent(sureButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(cancelButton);
		UnbindEvent(sureButton);
		UnbindEvent(closeButton);

		descText = null;
		loopGridView = null;
		cancelButton = null;
		sureButton = null;
		closeButton = null;
	}
}