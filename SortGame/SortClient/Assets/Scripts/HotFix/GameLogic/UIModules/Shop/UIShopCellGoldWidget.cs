using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIShopCellGoldWidget : ShopCellGoldBase, IShopCell
{
    private Tween currentScaleTween;
    cfg.Game.ShopCfg mCfg;
    bool mPriceReady;

    public object UpdateData(cfg.Game.ShopCfg oneCfg)
    {
        PlayShowAnim();

        int cnt = GameData.Shop.GetIntValueByItemStr(oneCfg.Award[0]);
        nameText.text = "x" + cnt;
        icon.sprite = LoadSprite("ShopIcon", GameData.Shop.GetCoinSpritePathByCnt(cnt));


        disacountNode.SetActiveEx(oneCfg.Discount > 0);
        disNumLb.text = oneCfg.Discount + "%";

        mCfg = oneCfg;
        mPriceReady = ShopPriceButton.Apply(priceText, buyBtn, GameData.Shop.GetPayDicId(oneCfg), LoadSprite("ShopIcon", "shopLoading"));
        return null;
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
}
