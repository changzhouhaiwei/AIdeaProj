using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ItemHeadBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/CommonHead/ItemHead.prefab";
    
	protected RectTransform animNode;
	protected TButton headButton_TButton;
	protected Image headButton_Image;
	protected Image iconImage;
	protected TextMeshProUGUI numberText;
	protected RectTransform progressBg;
	protected TextMeshProUGUI progressText;
	protected TextMeshProUGUI progressText2;
	protected Image selectBg;
	protected Image darkBg;
	protected TButton subtractButton;

    protected override void OnBindCompsAndEvents() 
    {
		animNode = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		headButton_TButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		headButton_Image = viewBehaviour.GetComponentByIndexs<Image>(1, 1);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
		numberText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		progressBg = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		progressText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		progressText2 = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		selectBg = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		darkBg = viewBehaviour.GetComponentByIndexs<Image>(8, 0);
		subtractButton = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);

		BindEvent(headButton_TButton);
		BindEvent(subtractButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(headButton_TButton);
		UnbindEvent(subtractButton);

		animNode = null;
		headButton_TButton = null;
		headButton_Image = null;
		iconImage = null;
		numberText = null;
		progressBg = null;
		progressText = null;
		progressText2 = null;
		selectBg = null;
		darkBg = null;
		subtractButton = null;
	}
}