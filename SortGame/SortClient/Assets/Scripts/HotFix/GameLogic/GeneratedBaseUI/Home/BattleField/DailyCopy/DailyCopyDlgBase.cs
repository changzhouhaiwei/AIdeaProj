using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyCopyDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/DailyCopy/DailyCopyDlg.prefab";
    
	protected TButton clearButton;
	protected TButton advancedClear;
	protected Image itemIm;
	protected TextMeshProUGUI clearNum;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		clearButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		advancedClear = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		itemIm = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		clearNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(4, 0);

		BindEvent(clearButton);
		BindEvent(advancedClear);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(clearButton);
		UnbindEvent(advancedClear);
		UnbindEvent(closeButton);

		clearButton = null;
		advancedClear = null;
		itemIm = null;
		clearNum = null;
		closeButton = null;
	}
}