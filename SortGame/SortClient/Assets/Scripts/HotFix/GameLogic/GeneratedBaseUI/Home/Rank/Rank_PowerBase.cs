using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class Rank_PowerBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Rank/Rank_Power.prefab";
    
	protected TextMeshProUGUI num;

    protected override void OnBindCompsAndEvents() 
    {
		num = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		num = null;
	}
}