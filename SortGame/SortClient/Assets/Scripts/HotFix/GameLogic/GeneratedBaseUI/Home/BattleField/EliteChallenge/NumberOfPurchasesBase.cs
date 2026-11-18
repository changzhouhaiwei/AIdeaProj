using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class NumberOfPurchasesBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/EliteChallenge/NumberOfPurchases.prefab";
    
	protected TextMeshProUGUI num;
	protected TextMeshProUGUI residueNum;
	protected TButton awardButton;
	protected Image itemIM;

    protected override void OnBindCompsAndEvents() 
    {
		num = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
		residueNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
		itemIM = viewBehaviour.GetComponentByIndexs<Image>(3, 0);

		BindEvent(awardButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardButton);

		num = null;
		residueNum = null;
		awardButton = null;
		itemIM = null;
	}
}