using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RankToggleBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Rank/RankToggle.prefab";
    
	protected TToggle rankToggle;
	protected TextMeshProUGUI label;

    protected override void OnBindCompsAndEvents() 
    {
		rankToggle = viewBehaviour.GetComponentByIndexs<TToggle>(0, 0);
		label = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);

		BindEvent(rankToggle);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(rankToggle);

		rankToggle = null;
		label = null;
	}
}