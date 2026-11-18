using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class RoleBigHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Role/RoleBigHead.prefab";
    
	protected RectTransform animNode;
	protected TButton headButton_TButton;
	protected Image headButton_Image;
	protected Image sx_Image;
	protected Image profession_Image;
	protected Image formation_Image;
	protected Image iconImage;
	protected Image starImage_1;
	protected Image starImage_2;
	protected Image starImage_3;
	protected Image starImage_4;
	protected Image starImage_5;
	protected TextMeshProUGUI levelText;
	protected TextMeshProUGUI nameText;

    protected override void OnBindCompsAndEvents() 
    {
		animNode = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		headButton_TButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		headButton_Image = viewBehaviour.GetComponentByIndexs<Image>(1, 1);
		sx_Image = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		profession_Image = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		formation_Image = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		starImage_1 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		starImage_2 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		starImage_3 = viewBehaviour.GetComponentByIndexs<Image>(8, 0);
		starImage_4 = viewBehaviour.GetComponentByIndexs<Image>(9, 0);
		starImage_5 = viewBehaviour.GetComponentByIndexs<Image>(10, 0);
		levelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(11, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(12, 0);

		BindEvent(headButton_TButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton_TButton);

		animNode = null;
		headButton_TButton = null;
		headButton_Image = null;
		sx_Image = null;
		profession_Image = null;
		formation_Image = null;
		iconImage = null;
		starImage_1 = null;
		starImage_2 = null;
		starImage_3 = null;
		starImage_4 = null;
		starImage_5 = null;
		levelText = null;
		nameText = null;
	}
}