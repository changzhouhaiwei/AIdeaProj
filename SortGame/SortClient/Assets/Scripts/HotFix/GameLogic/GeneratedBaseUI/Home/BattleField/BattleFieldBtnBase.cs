using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class BattleFieldBtnBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BattleField/BattleFieldBtn.prefab";
    
	protected TButton dailyCopyBtn_TButton;
	protected Image dailyCopyBtn_Image;
	protected TextMeshProUGUI time;
	protected TextMeshProUGUI dailyCopyText;
	protected TextMeshProUGUI dailyCopyTextNum;
	protected Image timeObj;
	protected Image timeNotObj;
	protected TextMeshProUGUI openTime;
	protected TextMeshProUGUI dailyCopyTime;

    protected override void OnBindCompsAndEvents() 
    {
		dailyCopyBtn_TButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		dailyCopyBtn_Image = viewBehaviour.GetComponentByIndexs<Image>(0, 1);
		time = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		dailyCopyText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		dailyCopyTextNum = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
		timeObj = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		timeNotObj = viewBehaviour.GetComponentByIndexs<Image>(5, 0);
		openTime = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(6, 0);
		dailyCopyTime = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(7, 0);

		BindEvent(dailyCopyBtn_TButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(dailyCopyBtn_TButton);

		dailyCopyBtn_TButton = null;
		dailyCopyBtn_Image = null;
		time = null;
		dailyCopyText = null;
		dailyCopyTextNum = null;
		timeObj = null;
		timeNotObj = null;
		openTime = null;
		dailyCopyTime = null;
	}
}