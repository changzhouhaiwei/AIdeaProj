using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class LotteryRecruitDisplayUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/LotteryRecruit/LotteryRecruitDisplayUI.prefab";
    
	protected Image mainBg;
	protected Image roleIcon;
	protected TextMeshProUGUI qualityText;
	protected TextMeshProUGUI nameText;
	protected Image starImage_1;
	protected Image starImage_2;
	protected Image starImage_3;
	protected Image starImage_4;
	protected Image starImage_5;
	protected Image sxImage;
	protected Image professionImage;
	protected Image professionPosImage;
	protected TextMeshProUGUI professionPosDesc;
	protected TButton touchButton;

    protected override void OnBindCompsAndEvents() 
    {
		mainBg = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		roleIcon = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		qualityText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		nameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		starImage_1 = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		starImage_2 = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		starImage_3 = viewBehaviour.GetComponentByIndexs<Image>(6, 0);
		starImage_4 = viewBehaviour.GetComponentByIndexs<Image>(7, 0);
		starImage_5 = viewBehaviour.GetComponentByIndexs<Image>(8, 0);
		sxImage = viewBehaviour.GetComponentByIndexs<Image>(9, 0);
		professionImage = viewBehaviour.GetComponentByIndexs<Image>(10, 0);
		professionPosImage = viewBehaviour.GetComponentByIndexs<Image>(11, 0);
		professionPosDesc = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(12, 0);
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(13, 0);

		BindEvent(touchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);

		mainBg = null;
		roleIcon = null;
		qualityText = null;
		nameText = null;
		starImage_1 = null;
		starImage_2 = null;
		starImage_3 = null;
		starImage_4 = null;
		starImage_5 = null;
		sxImage = null;
		professionImage = null;
		professionPosImage = null;
		professionPosDesc = null;
		touchButton = null;
	}
}