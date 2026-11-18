using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class BottomDotBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/BottomDot.prefab";
    
	protected Image normalImage;
	protected Image selectImage;
	protected TButton emptyButton;

    protected override void OnBindCompsAndEvents() 
    {
		normalImage = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		selectImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		emptyButton = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);

		BindEvent(emptyButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(emptyButton);

		normalImage = null;
		selectImage = null;
		emptyButton = null;
	}
}