using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class UserFrameNodeBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UICommon/UserFrameNode.prefab";
    
	protected TextMeshProUGUI totalWinCount;

    protected override void OnBindCompsAndEvents() 
    {
		totalWinCount = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		totalWinCount = null;
	}
}
