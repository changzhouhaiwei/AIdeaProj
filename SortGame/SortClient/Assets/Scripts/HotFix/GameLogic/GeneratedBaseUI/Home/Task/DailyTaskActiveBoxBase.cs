using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class DailyTaskActiveBoxBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Task/DailyTaskActiveBox.prefab";
    
	protected Image boxImage;
	protected TButton touchButton;
	protected RedDotUI redDotUI;

    protected override void OnBindCompsAndEvents() 
    {
		boxImage = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
		redDotUI = viewBehaviour.GetComponentByIndexs<RedDotUI>(2, 0);

		BindEvent(touchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);

		boxImage = null;
		touchButton = null;
		redDotUI = null;
	}
}