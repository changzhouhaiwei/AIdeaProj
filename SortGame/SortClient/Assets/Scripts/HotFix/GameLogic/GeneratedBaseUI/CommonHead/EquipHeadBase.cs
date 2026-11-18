using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class EquipHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/CommonHead/EquipHead.prefab";
    
	protected RectTransform animNode;
	protected TButton headButton_TButton;
	protected Image headButton_Image;
	protected Image iconImage;
	protected TextMeshProUGUI levelText;
	protected RectTransform starBg;
	protected Image starImage_1;
	protected Image starImage_2;
	protected Image starImage_3;
	protected Image starImage_4;
	protected Image starImage_5;
	protected Image selectBg;
	protected Image darkBg;

    protected override void OnBindCompsAndEvents() 
    {
		animNode = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		headButton_TButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		headButton_Image = viewBehaviour.GetComponentByIndexs<Image>(1, 1);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		levelText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		starBg = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		starImage_1 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		starImage_2 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		starImage_3 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		starImage_4 = viewBehaviour.GetComponentByIndexs<Image>(8, 0);
		starImage_5 = viewBehaviour.GetComponentByIndexs<Image>(9, 0);
		selectBg = viewBehaviour.GetComponentByIndexs<Image>(10, 0);
		darkBg = viewBehaviour.GetComponentByIndexs<Image>(11, 0);

		BindEvent(headButton_TButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton_TButton);

		animNode = null;
		headButton_TButton = null;
		headButton_Image = null;
		iconImage = null;
		levelText = null;
		starBg = null;
		starImage_1 = null;
		starImage_2 = null;
		starImage_3 = null;
		starImage_4 = null;
		starImage_5 = null;
		selectBg = null;
		darkBg = null;
	}
}