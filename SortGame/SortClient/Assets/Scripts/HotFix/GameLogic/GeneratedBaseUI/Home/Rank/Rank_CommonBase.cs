using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class Rank_CommonBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Rank/Rank_Common.prefab";
    
	protected TextMeshProUGUI rankText;

    protected override void OnBindCompsAndEvents() 
    {
		rankText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		rankText = null;
	}
}