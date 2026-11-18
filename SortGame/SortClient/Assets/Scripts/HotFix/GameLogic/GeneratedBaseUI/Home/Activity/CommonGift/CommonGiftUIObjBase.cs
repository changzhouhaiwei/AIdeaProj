using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CommonGiftUIObjBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/CommonGift/CommonGiftUIObj.prefab";
    
	protected UIWidgetBehaviour commonGiftCell;
	protected Image bG;
	protected Image timeBg;
	protected TextMeshProUGUI time;
	protected RectTransform content;

    protected override void OnBindCompsAndEvents() 
    {
		commonGiftCell = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		bG = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		timeBg = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		content = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		commonGiftCell = null;
		bG = null;
		timeBg = null;
		time = null;
		content = null;
	}
}