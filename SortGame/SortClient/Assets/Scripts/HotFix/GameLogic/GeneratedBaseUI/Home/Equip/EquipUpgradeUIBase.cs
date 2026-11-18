using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipUpgradeUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Equip/EquipUpgradeUI.prefab";
    
	protected UIWidgetBehaviour equipHeadObj;
	protected Image filledImage;
	protected TextMeshProUGUI progresText;
	protected TextMeshProUGUI maxText;
	protected TextMeshProUGUI fromLevelText;
	protected TextMeshProUGUI toLevelText;
	protected TextMeshProUGUI toStrengthText;
	protected TextMeshProUGUI fromStrengthText;
	protected TextMeshProUGUI fromIntelligenceText;
	protected TextMeshProUGUI toIntelligenceText;
	protected TextMeshProUGUI fromConstitutionText;
	protected TextMeshProUGUI toConstitutionText;
	protected TButton autoUpgradeButton;
	protected RectTransform goldBg;
	protected Image goldImage;
	protected TextMeshProUGUI goldCostText;
	protected TButton upgradeButton;
	protected LoopGridView loopGridView;

    protected override void OnBindCompsAndEvents() 
    {
		equipHeadObj = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 0);
		filledImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		progresText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		maxText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		fromLevelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		toLevelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		toStrengthText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		fromStrengthText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);
		fromIntelligenceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		toIntelligenceText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(9, 0);
		fromConstitutionText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(10, 0);
		toConstitutionText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(11, 0);
		autoUpgradeButton = viewBehaviour.GetComponentByIndexs<TButton>(12, 0);
		goldBg = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);
		goldImage = viewBehaviour.GetComponentByIndexs<Image>(14, 0);
		goldCostText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(15, 0);
		upgradeButton = viewBehaviour.GetComponentByIndexs<TButton>(16, 0);
		loopGridView = viewBehaviour.GetComponentByIndexs<LoopGridView>(17, 0);

		BindEvent(autoUpgradeButton);
		BindEvent(upgradeButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(autoUpgradeButton);
		UnbindEvent(upgradeButton);

		equipHeadObj = null;
		filledImage = null;
		progresText = null;
		maxText = null;
		fromLevelText = null;
		toLevelText = null;
		toStrengthText = null;
		fromStrengthText = null;
		fromIntelligenceText = null;
		toIntelligenceText = null;
		fromConstitutionText = null;
		toConstitutionText = null;
		autoUpgradeButton = null;
		goldBg = null;
		goldImage = null;
		goldCostText = null;
		upgradeButton = null;
		loopGridView = null;
	}
}