using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class TrialBlessDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/Trial/TrialBlessDlg.prefab";
    
	protected RectTransform skill;
	protected RectTransform skillUp;
	protected TextMeshProUGUI condition;
	protected TextMeshProUGUI skillText;
	protected TextMeshProUGUI skillUpText;
	protected TButton closeButton;

    protected override void OnBindCompsAndEvents() 
    {
		skill = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		skillUp = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		condition = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		skillText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		skillUpText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(5, 0);

		BindEvent(closeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(closeButton);

		skill = null;
		skillUp = null;
		condition = null;
		skillText = null;
		skillUpText = null;
		closeButton = null;
	}
}