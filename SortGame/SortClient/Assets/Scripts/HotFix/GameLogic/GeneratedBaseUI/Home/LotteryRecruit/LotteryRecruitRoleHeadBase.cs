using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitRoleHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitRoleHead.prefab";
    
	protected TButton headButton_TButton;
	protected Image headButton_Image;
	protected Image sx_Image;
	protected Image profession_Image;
	protected Image iconImage;
	protected Image starImage_1;
	protected Image starImage_2;
	protected Image starImage_3;
	protected Image starImage_4;
	protected Image starImage_5;
	protected TextMeshProUGUI levelText;
	protected TextMeshProUGUI nameText;
	protected Image qualityImage;
	protected RectTransform newImage;
	protected RectTransform decomposeBg;

    protected override void OnBindCompsAndEvents() 
    {
		headButton_TButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		headButton_Image = viewBehaviour.GetComponentByIndexs<Image>(0, 1);
		sx_Image = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		profession_Image = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
		starImage_1 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		starImage_2 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		starImage_3 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		starImage_4 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		starImage_5 = viewBehaviour.GetComponentByIndexs<Image>(8, 0);
		levelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(9, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(10, 0);
		qualityImage = viewBehaviour.GetComponentByIndexs<Image>(11, 0);
		newImage = viewBehaviour.GetComponentByIndexs<RectTransform>(12, 0);
		decomposeBg = viewBehaviour.GetComponentByIndexs<RectTransform>(13, 0);

		BindEvent(headButton_TButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton_TButton);

		headButton_TButton = null;
		headButton_Image = null;
		sx_Image = null;
		profession_Image = null;
		iconImage = null;
		starImage_1 = null;
		starImage_2 = null;
		starImage_3 = null;
		starImage_4 = null;
		starImage_5 = null;
		levelText = null;
		nameText = null;
		qualityImage = null;
		newImage = null;
		decomposeBg = null;
	}
}