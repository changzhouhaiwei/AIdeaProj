using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UIStoryBookViewBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/TUIStory/UIStoryBookView.prefab";
    
	protected TButton backButton;
	protected UIWidgetBehaviour storyCellNode_UIWidgetBehaviour;
	protected RectTransform storyCellNode_RectTransform;
	protected Animation uIStoryBookView;

    protected override void OnBindCompsAndEvents() 
    {
		backButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
		storyCellNode_UIWidgetBehaviour = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(1, 0);
		storyCellNode_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(1, 1);
		uIStoryBookView = viewBehaviour.GetComponentByIndexs<Animation>(2, 0);

		BindEvent(backButton);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(backButton);

		backButton = null;
		storyCellNode_UIWidgetBehaviour = null;
		storyCellNode_RectTransform = null;
		uIStoryBookView = null;
	}
}