using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class NetworkErrorDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/NetworkErrorDlg.prefab";
    
	protected RectTransform nineBg;
	protected TButton sureButton;
	protected TextMeshProUGUI connectingLb;

    protected override void OnBindCompsAndEvents() 
    {
		nineBg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		sureButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		connectingLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);

		BindEvent(sureButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(sureButton);

		nineBg = null;
		sureButton = null;
		connectingLb = null;
	}
}