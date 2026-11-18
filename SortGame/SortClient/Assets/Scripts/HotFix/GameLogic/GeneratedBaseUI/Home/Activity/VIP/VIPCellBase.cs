using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class VIPCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/VIP/VIPCell.prefab";
    
	protected Image vIPNow;
	protected TextMeshProUGUI vIPNowNum;
	protected TextMeshProUGUI vIPNum;
	protected TButton btnVIP;
	protected Image image;

    protected override void OnBindCompsAndEvents() 
    {
		vIPNow = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		vIPNowNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		vIPNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		btnVIP = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		image = viewBehaviour.GetComponentByIndexs<Image>(4, 0);

		BindEvent(btnVIP);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnVIP);

		vIPNow = null;
		vIPNowNum = null;
		vIPNum = null;
		btnVIP = null;
		image = null;
	}
}