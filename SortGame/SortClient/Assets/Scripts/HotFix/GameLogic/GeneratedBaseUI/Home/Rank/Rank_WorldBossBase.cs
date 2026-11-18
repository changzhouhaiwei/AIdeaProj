using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class Rank_WorldBossBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Rank/Rank_WorldBoss.prefab";
    
	protected RectTransform roleRect;
	protected TextMeshProUGUI num;

    protected override void OnBindCompsAndEvents() 
    {
		roleRect = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		num = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		roleRect = null;
		num = null;
	}
}