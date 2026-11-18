using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class AssetNodeBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/AssetNode.prefab";
    
	protected RectTransform bg;
	protected Image iconImage;
	protected TextMeshProUGUI cntText;
	protected TButton plusButton;
	protected RectTransform timeImage;
	protected TextMeshProUGUI timeText;

    protected override void OnBindCompsAndEvents() 
    {
		bg = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		iconImage = viewBehaviour.GetComponentByIndexs<Image>(1, 0);
		cntText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		plusButton = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		timeImage = viewBehaviour.GetComponentByIndexs<RectTransform>(4, 0);
		timeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);

		BindEvent(plusButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(plusButton);

		bg = null;
		iconImage = null;
		cntText = null;
		plusButton = null;
		timeImage = null;
		timeText = null;
	}
}