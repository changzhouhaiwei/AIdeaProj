using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LoadingUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UILogin/LoadingUI.prefab";
    
	protected TextMeshProUGUI loadingLb;
	protected Image loadingBar;

    protected override void OnBindCompsAndEvents() 
    {
		loadingLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		loadingBar = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		loadingLb = null;
		loadingBar = null;
	}
}