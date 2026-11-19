
using UnityEngine.UI;
using cfg.Game;

public class UIStoryInfoDlg : UIStoryInfoDlgBase
{
    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == backButton)
        {
            CloseSelf();
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
    }

    public void SetCellData(GameBgCfg cfg)
    {
        titleText.text = I2LocalizeMgr.GetString(cfg.Name);
        descLb.text = I2LocalizeMgr.GetString(cfg.Desc);
        name2Text.text = I2LocalizeMgr.GetString(cfg.Name2);
        bg.sprite = LoadSpriteBg("SceneBg", "bg_" + cfg.Path);
    }
}