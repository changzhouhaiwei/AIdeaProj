using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class BoxAwardTipBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/BoxAwardTip.prefab";
    
	protected TButton touchButton;
	protected RectTransform nineBg;

    protected override void OnBindCompsAndEvents() 
    {
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		nineBg = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 0);

		BindEvent(touchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);

		touchButton = null;
		nineBg = null;
	}
}