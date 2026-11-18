using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class FristTopUpBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/TopUp/FristTopUp.prefab";
    
	protected RectTransform rewards;
	protected TButton awardButton;
	protected TextMeshProUGUI awardText;

    protected override void OnBindCompsAndEvents() 
    {
		rewards = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		awardText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);

		BindEvent(awardButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(awardButton);

		rewards = null;
		awardButton = null;
		awardText = null;
	}
}