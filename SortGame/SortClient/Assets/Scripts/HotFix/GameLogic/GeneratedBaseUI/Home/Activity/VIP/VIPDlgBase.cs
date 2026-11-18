using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class VIPDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/VIP/VIPDlg.prefab";
    
	protected TextMeshProUGUI titleText;
	protected RectTransform itemRect;
	protected TButton clearButton;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		titleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		itemRect = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		clearButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);

		BindEvent(clearButton);
		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(clearButton);
		UnbindEvent(closeButton);

		titleText = null;
		itemRect = null;
		clearButton = null;
		closeButton = null;
	}
}