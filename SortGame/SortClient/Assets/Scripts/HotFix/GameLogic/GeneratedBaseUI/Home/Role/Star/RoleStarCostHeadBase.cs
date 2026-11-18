using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleStarCostHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/Star/RoleStarCostHead.prefab";
    
	protected TButton headButton;
	protected RectTransform iconImage;
	protected Image sx_Image;
	protected Image starImage_1;
	protected Image starImage_2;
	protected Image starImage_3;
	protected Image starImage_4;
	protected Image starImage_5;
	protected TextMeshProUGUI progressText;
	protected TextMeshProUGUI descText;

    protected override void OnBindCompsAndEvents() 
    {
		headButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);
		sx_Image = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		starImage_1 = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		starImage_2 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		starImage_3 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		starImage_4 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		starImage_5 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		progressText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		descText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(9, 0);

		BindEvent(headButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton);

		headButton = null;
		iconImage = null;
		sx_Image = null;
		starImage_1 = null;
		starImage_2 = null;
		starImage_3 = null;
		starImage_4 = null;
		starImage_5 = null;
		progressText = null;
		descText = null;
	}
}