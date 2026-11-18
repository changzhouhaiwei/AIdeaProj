using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CommonDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/CommonDlg.prefab";
    
	protected TextMeshProUGUI titleText;
	protected TextMeshProUGUI descText;
	protected TButton sureButton;

    protected override void OnBindCompsAndEvents() 
    {
		titleText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(sureButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sureButton);

		titleText = null;
		descText = null;
		sureButton = null;
	}
}