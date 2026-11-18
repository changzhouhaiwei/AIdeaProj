using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;

public class AlertDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/AlertDlg.prefab";
    
	protected TextMeshProUGUI tipText;
	protected RectTransform nineBg_RectTransform;
	protected CanvasGroup nineBg_CanvasGroup;

    protected override void OnBindCompsAndEvents() 
    {
		tipText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		nineBg_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		nineBg_CanvasGroup = viewBehaviour.GetComponentByIndexs<CanvasGroup>(1, 1);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		tipText = null;
		nineBg_RectTransform = null;
		nineBg_CanvasGroup = null;
	}
}