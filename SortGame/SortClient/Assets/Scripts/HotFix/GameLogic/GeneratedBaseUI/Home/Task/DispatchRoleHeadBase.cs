using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DispatchRoleHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/DispatchRoleHead.prefab";
    
	protected TButton headButton;
	protected RectTransform enemyImage;
	protected Image iconImage;
	protected Image qualityImage;
	protected Image sxImage;
	protected TextMeshProUGUI lvText;

    protected override void OnBindCompsAndEvents() 
    {
		headButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		enemyImage = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		qualityImage = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		sxImage = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		lvText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);

		BindEvent(headButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton);

		headButton = null;
		enemyImage = null;
		iconImage = null;
		qualityImage = null;
		sxImage = null;
		lvText = null;
	}
}