using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class CommonGiftCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Activity/CommonGift/CommonGiftCell.prefab";
    
	protected TextMeshProUGUI name;
	protected RectTransform reward;
	protected TextMeshProUGUI content;
	protected TButton btnReward;
	protected TextMeshProUGUI priceText;
	protected Image mask;
	protected Image image;
	protected Image bG;

    protected override void OnBindCompsAndEvents() 
    {
		name = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		reward = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		content = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		btnReward = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		priceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		mask = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		image = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		bG = viewBehaviour.GetComponentByIndexs<Image>(7, 0);

		BindEvent(btnReward);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnReward);

		name = null;
		reward = null;
		content = null;
		btnReward = null;
		priceText = null;
		mask = null;
		image = null;
		bG = null;
	}
}