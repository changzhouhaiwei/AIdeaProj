using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class PlayerDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/Home/Player/PlayerDlg.prefab";
    
	protected Image headIcon;
	protected TextMeshProUGUI lvText;
	protected TextMeshProUGUI playerName;
	protected TButton btnRename;
	protected Image expImageBar;
	protected TextMeshProUGUI powerText;
	protected TButton awardButton;
	protected TButton closeButton;
	protected TextMeshProUGUI uid;
	protected Slider musicSlider;
	protected Slider soundSlider;
	protected TextMeshProUGUI plateNnumber;

    protected override void OnBindCompsAndEvents() 
    {
		headIcon = viewBehaviour.GetComponentByIndexs<Image>(0, 0);
		lvText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
		playerName = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
		btnRename = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
		expImageBar = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
		powerText = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
		awardButton = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
		closeButton = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
		uid = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(8, 0);
		musicSlider = viewBehaviour.GetComponentByIndexs<Slider>(9, 0);
		soundSlider = viewBehaviour.GetComponentByIndexs<Slider>(10, 0);
		plateNnumber = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(11, 0);

		BindEvent(btnRename);
		BindEvent(awardButton);
		BindEvent(closeButton);
		BindEvent(musicSlider);
		BindEvent(soundSlider);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(btnRename);
		UnbindEvent(awardButton);
		UnbindEvent(closeButton);
		UnbindEvent(musicSlider);
		UnbindEvent(soundSlider);

		headIcon = null;
		lvText = null;
		playerName = null;
		btnRename = null;
		expImageBar = null;
		powerText = null;
		awardButton = null;
		closeButton = null;
		uid = null;
		musicSlider = null;
		soundSlider = null;
		plateNnumber = null;
	}
}