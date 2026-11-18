
using UnityEngine;
using DG.Tweening;
using System.Linq;
using Core.Tools.MethodExtension;
using TMPro;
using UnityEngine.UI;
using FishFramework;
using System.Collections.Generic;

public class UIShopCellBigWidget : ShopCellBigBase, IShopCell
{
    private Tween currentScaleTween;
    List<GameObject> objList = new();
    cfg.Game.ShopCfg mCfg;
    bool mPriceReady;

    public object UpdateData(cfg.Game.ShopCfg oneCfg)
    {
        mCfg = oneCfg;
        PlayShowAnim();
        UpdateIconBg(mCfg.Id);
        ShowAwards();

        nameText.text = I2LocalizeMgr.GetString(oneCfg.Name);

        disacountNode.SetActiveEx(oneCfg.Discount > 0);
        disNumLb.text = oneCfg.Discount + "%";

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

    void UpdateIconBg(int id)
    {
        cellBg1.SetActiveEx(id == 1);
        cellBg2.SetActiveEx(id == 2);
        cellBg3.SetActiveEx(id == 3);
        cellBg4.SetActiveEx(id == 4);

        icon1.SetActiveEx(id == 1);
        icon2.SetActiveEx(id == 2);
        icon3.SetActiveEx(id == 3);
        icon4.SetActiveEx(id == 4);
    }

    Image GetActiveBundleIcon()
    {
        return mCfg.Id switch
        {
            1 => icon1,
            2 => icon2,
            3 => icon3,
            4 => icon4,
            _ => icon1
        };
    }

    void ShowAwards()
    {
        foreach (var item in objList)
        {
           GameObject.Destroy(item);
        }

        objList.Clear();
        
        oriAwardCell.SetActiveEx(true);
        foreach (var item in mCfg.Award)
        {
            var arr = item.Split(",").ToList();
            if (arr.Count == 2)
            {
                var id = arr[0].ToInt();
                var num = arr[1].ToInt();

                if (id == 4)
                {
                    var bundleIcon = GetActiveBundleIcon();
                    var coinText = bundleIcon.GetComponentInChildren<TextMeshProUGUI>();
                    if (coinText != null)
                    {
                        coinText.text = num.ToString();
                    }
                    continue;
                }

                Transform pNode = oriAwardCell.transform.parent;
                var newNode = Transform.Instantiate(oriAwardCell, pNode);
                objList.Add(newNode.gameObject);
                var icon = newNode.gameObject.GetComponentInChildren<Image>();
                var textMesh = newNode.gameObject.GetComponentInChildren<TextMeshProUGUI>();

                icon.sprite = LoadSprite("LevelAward", "item_" + id);
                textMesh.text = "x" + num;
            }

        }
        oriAwardCell.SetActiveEx(false);

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