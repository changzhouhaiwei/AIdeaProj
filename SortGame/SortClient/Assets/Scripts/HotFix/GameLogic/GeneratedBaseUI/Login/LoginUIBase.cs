using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LoginUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UILogin/LoginUI.prefab";
    
	protected TextMeshProUGUI userDataVer;
	protected Image loadingBar;
	protected TextMeshProUGUI loadingLb;
	protected TextMeshProUGUI quateDescLb;
	protected TextMeshProUGUI quateNameLb;
	protected Image loadingBg;

    protected override void OnBindCompsAndEvents() 
    {
		userDataVer = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		loadingBar = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		loadingLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		quateDescLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		quateNameLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		loadingBg = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		userDataVer = null;
		loadingBar = null;
		loadingLb = null;
		quateDescLb = null;
		quateNameLb = null;
		loadingBg = null;
	}
}