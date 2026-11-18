using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ExchangeRankCellBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/WorldBoss/ExchangeRankCell.prefab";
    
	protected RectTransform fourth;
	protected RectTransform first;
	protected RectTransform second;
	protected RectTransform third;
	protected TextMeshProUGUI index;
	protected RectTransform itemRect;

    protected override void OnBindCompsAndEvents() 
    {
		fourth = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		first = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		second = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		third = viewBehaviour.GetComponentByIndexs<RectTransform>(3, 0);
		index = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		itemRect = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		fourth = null;
		first = null;
		second = null;
		third = null;
		index = null;
		itemRect = null;
	}
}