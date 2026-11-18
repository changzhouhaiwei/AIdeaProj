using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ChoseEquipDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/ChoseEquipDlg.prefab";
    
	protected TextMeshProUGUI defautText;
	protected UIWidgetBehaviour choseEquipCellObj;
	protected LoopListView2 loopListView;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		defautText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		choseEquipCellObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		loopListView = viewBehaviour.GetComponentByIndexs<LoopListView2>(2, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		defautText = null;
		choseEquipCellObj = null;
		loopListView = null;
		closeButton = null;
	}
}