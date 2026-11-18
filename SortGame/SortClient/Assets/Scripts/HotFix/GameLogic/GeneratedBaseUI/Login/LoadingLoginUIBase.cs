using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LoadingLoginUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UILogin/LoadingLoginUI.prefab";
    
	protected TextMeshProUGUI quateNameLb;
	protected TextMeshProUGUI quateDescLb;
	protected Image loadingBar;

    protected override void OnBindCompsAndEvents() 
    {
		quateNameLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		quateDescLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		loadingBar = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		quateNameLb = null;
		quateDescLb = null;
		loadingBar = null;
	}
}