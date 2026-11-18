using System;
using UnityEngine.UI;

public class UIShopCellSpreadWidget : ShopCellSpreadBase, IShopCell
{
    public object UpdateData(cfg.Game.ShopCfg oneCfg)
    {
        return null;
    }

    Action mBtnAct;

    public void SetClickAction(Action act)
    {
        mBtnAct = act;
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.buttonSoundSpread);
        
        if (button == spreadBtn)
        {
            mBtnAct?.Invoke();
        }
    }
    public void PlayShowAnim(bool bDelay, int index)
    {

    }
}