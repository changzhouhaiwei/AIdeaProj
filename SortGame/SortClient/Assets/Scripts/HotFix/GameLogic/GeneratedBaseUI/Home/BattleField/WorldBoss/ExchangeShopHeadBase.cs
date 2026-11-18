using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ExchangeShopHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/WorldBoss/ExchangeShopHead.prefab";
    
	protected TextMeshProUGUI name;
	protected TButton buyBtn;
	protected TextMeshProUGUI buyNum;
	protected Image itemIm;
	protected RectTransform item;
	protected Image alreadyImage;

    protected override void OnBindCompsAndEvents() 
    {
		name = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		buyBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		buyNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		itemIm = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		item = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		alreadyImage = viewBehaviour.GetComponentByIndexs<Image>(5, 0);

		BindEvent(buyBtn);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(buyBtn);

		name = null;
		buyBtn = null;
		buyNum = null;
		itemIm = null;
		item = null;
		alreadyImage = null;
	}
}