using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIStoryCellNodeBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/TUIStory/UIStoryCellNode.prefab";
    
	protected RectTransform storyGridNode_RectTransform;
	protected UIWidgetBehaviour storyGridNode_UIWidgetBehaviour;
	protected TextMeshProUGUI countryNameText;
	protected RectTransform finishSp;
	protected TextMeshProUGUI levelRangeText;

    protected override void OnBindCompsAndEvents() 
    {
		storyGridNode_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
		storyGridNode_UIWidgetBehaviour = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(0, 1);
		countryNameText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		finishSp = viewBehaviour.GetComponentByIndexs<RectTransform>(2, 0);
		levelRangeText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(3, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		storyGridNode_RectTransform = null;
		storyGridNode_UIWidgetBehaviour = null;
		countryNameText = null;
		finishSp = null;
		levelRangeText = null;
	}
}