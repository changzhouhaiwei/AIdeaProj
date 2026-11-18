
using System;
using UnityEngine.UI;

public class UserHead : UserHeadNodeBase
{
    public Action<UserHead> OnClickCb;

    private int mHeadId;
    private int mFrameId;
    private bool mbSelected;

    public int GetHeadId()
    {
        return mHeadId;
    }

    public int GetFrameId()
    {
        return mFrameId;
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        SetBSelected(false);
    }

    public void UpdateFrame(int id)
    {
        mFrameId = id;
        if (frame == null)
        {
            return;
        }

        if (id <= 0)
        {
            frame.gameObject.SetActiveEx(false);
            return;
        }

        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(id);
        if (cfg == null)
        {
            frame.gameObject.SetActiveEx(false);
            return;
        }

        frame.gameObject.SetActiveEx(true);
        frame.sprite = LoadAtlasSprite("UserInfo", cfg.Pic);
    }

    public void SetBSelected(bool b)
    {
        mbSelected = b;
        if (bSel != null)
        {
            bSel.SetActiveEx(b);
        }
    }

    public void UpdateIcon(int id)
    {
        mHeadId = id;
        if (icon != null)
        {
            icon.sprite = LoadAtlasSprite("UserInfo", "Avatar_" + id);
        }
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == emptyButton)
        {
            OnClickCb?.Invoke(this);
        }
    }
}
