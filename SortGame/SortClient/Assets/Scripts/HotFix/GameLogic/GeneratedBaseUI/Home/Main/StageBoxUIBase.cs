using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class StageBoxUIBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Main/StageBoxUI.prefab";
    
	protected Image boxImage;
	protected TextMeshProUGUI dayText;
	protected TButton touchButton;

    protected override void OnBindCompsAndEvents() 
    {
		boxImage = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		dayText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		touchButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(touchButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(touchButton);

		boxImage = null;
		dayText = null;
		touchButton = null;
	}
}