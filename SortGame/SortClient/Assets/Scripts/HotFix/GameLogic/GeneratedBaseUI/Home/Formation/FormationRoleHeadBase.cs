using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class FormationRoleHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Formation/FormationRoleHead.prefab";
    
	protected TButton headButton;
	protected RectTransform enemyImage;
	protected Image iconImage;
	protected Image qualityImage;
	protected TextMeshProUGUI lvText;
	protected RectTransform captainImage;

    protected override void OnBindCompsAndEvents() 
    {
		headButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		enemyImage = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		qualityImage = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		lvText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
		captainImage = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 0);

		BindEvent(headButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton);

		headButton = null;
		enemyImage = null;
		iconImage = null;
		qualityImage = null;
		lvText = null;
		captainImage = null;
	}
}