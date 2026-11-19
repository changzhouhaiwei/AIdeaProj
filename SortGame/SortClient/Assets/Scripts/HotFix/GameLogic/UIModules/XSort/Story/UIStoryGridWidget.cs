using cfg.Game;
using XSortGameCore;
using UnityEngine.UI;
using FishFramework;

public class UIStoryGridWidget : UIStoryGridNodeBase
{
    private GameBgCfg m_cfg;
    private bool m_bLock;
    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);

        if (button == bgButton)
        {
            if (!m_bLock)
            {
                var dlg = GameModule.UI.OpenPanel<UIStoryInfoDlg>();
                dlg.SetCellData(m_cfg);
            }
            else
            {
                AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UIPhotoAlbumPhotoLockTips"));
            }
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
    }

    public void SetCellData(GameBgCfg cfg)
    {
        // nameLb.text = I2LocalizeMgr.GetString(cfg.Name);
        m_cfg = cfg;

        int startLevel = cfg.StartLevel;
        int endLevel = cfg.EndLevel;

        int curLevel = GPUserDataManager.Instance.GetLevelNum() + 1;
        bool bLock = curLevel < startLevel;

        SetBlock(bLock);
    }

    public void SetBlock(bool b)
    {
        m_bLock = b;
        lockSp.SetActiveEx(b);
        if (b)
        {
            // UIUtils.SetImageGray(icon);
            icon.sprite = LoadSprite("SceneIcon", "dark");
        }
        else
        {
            icon.sprite = LoadSprite("SceneIcon", m_cfg.Path);

            // UIUtils.ResetImageGray(icon);
        }
    }
}