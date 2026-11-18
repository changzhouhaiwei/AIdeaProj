using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIShopCellNoAdsWidget : ShopCellNoAdsBase, IShopCell
{
    private Tween currentScaleTween;
    cfg.Game.ShopCfg mCfg;
    bool mPriceReady;
    public object UpdateData(cfg.Game.ShopCfg oneCfg)
    {
        PlayShowAnim();
        descText.text = I2LocalizeMgr.GetString(oneCfg.Name);


        disacountNode.SetActiveEx(oneCfg.Discount > 0);
        disNumLb.text = oneCfg.Discount + "%";

        if (oneCfg.Award.Count > 0)
        {
            coinNum.text = GameData.Shop.GetIntValueByItemStr(oneCfg.Award[0]).ToString();
        }

        mPriceReady = ShopPriceButton.Apply(priceText, buyBtn, GameData.Shop.GetPayDicId(oneCfg), LoadSprite("ShopIcon", "shopLoading"));

        mCfg = oneCfg;
        
        return null;
    }

    public void PlayShowAnim(bool bDelay = false, int index = 0)
    {
        // 停止上一次的动画
        if (currentScaleTween != null)
        {
            currentScaleTween.Kill();
            currentScaleTween = null;
        }

        // 设置初始缩放为 0.8
        gameObject.transform.localScale = Vector3.one * 0.8f;

        // 创建从 0.8 到 1.0 的缩放动画
        currentScaleTween = gameObject.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                currentScaleTween = null;
                gameObject.GetComponent<TButton>()?.SetRestingScale(Vector3.one);
            });
    }


    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == buyBtn)
        {
            if (!mPriceReady)
            {
                return;
            }
#if UNITY_EDITOR
            UIGameFunc.SimulateEditorIapSuccess(GameData.Shop.GetPayDicId(mCfg).ToString());
#else
            GameData.Shop.CheckBuy(mCfg);
#endif
        }
    }
}