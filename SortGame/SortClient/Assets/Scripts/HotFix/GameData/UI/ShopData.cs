using System.Collections.Generic;
using System.Linq;
using cfg.Game;
using Core.Tools.MethodExtension;
using FishFramework;

public class ShopData : BaseData
{
    public class ShopAssetData
    {
        public int id;
        public int cnt;
    }

    List<cfg.Game.ShopCfg> mCfgList;
    public override void OnInit()
    {
        var dataList = ConfigHelper.Tables.TbShopCfg.DataList;
        dataList.Sort((a, b) => a.Id.CompareTo(b.Id));
        mCfgList = dataList;


    }



    public override void OnReset()
    {
    }

    public override void OnRemove()
    {

    }

    public string GetCoinSpritePathByCnt(int cnt)
    {
        var dataList = ConfigHelper.Tables.TbShopCoinShowCfg.DataList;
        dataList.Sort((a, b) => a.Id.CompareTo(b.Id));
        var maxCfg = dataList.Last();
        if (cnt >= maxCfg.StartNum)
        {
            return maxCfg.Icon;
        }

        foreach (var item in dataList)
        {
            if (cnt >= item.StartNum && cnt <= item.EndNum)
            {
                return item.Icon;
            }
        }

        return "shop_jb_1";
    }

    public List<cfg.Game.ShopCfg> GetShopCfgList(bool bSpread = true)
    {
        List<cfg.Game.ShopCfg> retList = new(mCfgList);

        if (bSpread)
        {
            // return mCfgList;
        }
        else
        {
            retList = mCfgList.FindAll((a) => a.BSpreadShow == 0);
            // return mCfgList.FindAll((a) => a.BSpreadShow == 0);
        }

        foreach (var item in retList)
        {
            if (GetPayDicId(item) == GameData.Pay.GetNoAdsPayId().ToInt() && GPUserDataManager.Instance.GetBNoAd())
            {
                retList.Remove(item);
                break;
            }
        }

        retList.Sort((a, b) =>
        {
            bool aIsNoAds = a.ShopCellType == 2;
            bool bIsNoAds = b.ShopCellType == 2;
            if (aIsNoAds != bIsNoAds)
            {
                return aIsNoAds ? -1 : 1;
            }

            return a.Id.CompareTo(b.Id);
        });

        return retList;
    }

    public int GetIntValueByItemStr(string str)
    {
        var list = str.Split(",").ToList();
        if (list.Count > 1)
        {
            return list[1].ToInt();
        }
        return 0;
    }

    public void ExcuteGetShopCfgAward(cfg.Game.ShopCfg cfg)
    {
        var award = cfg.Award;
    }

    public cfg.Game.ShopCfg GetCfgBuyGID(string gid)
    {
        var payCfgList = ConfigHelper.Tables.TbPayProductCfg.DataList;
        var oneCfg = payCfgList.Find((a) => GameData.Pay.MatchesStoreProductId(a, gid));
        if (oneCfg == null)
        {
            return null;
        }

        var shopCfgList = ConfigHelper.Tables.TbShopCfg.DataList;
        var oneShopCfg = shopCfgList.Find((a) => GetPayDicId(a) == oneCfg.Id);

        return oneShopCfg;
    }

    public cfg.Game.ShopCfg GetCfgBuyPayID(string payId)
    {
        if (string.IsNullOrEmpty(payId))
        {
            return null;
        }

        // 支持直接传商店商品ID（如 soeasytile.super）。
        if (!int.TryParse(payId, out int payDicId))
        {
            return GetCfgBuyGID(payId);
        }

        var shopCfgList = ConfigHelper.Tables.TbShopCfg.DataList;
        var oneShopCfg = shopCfgList.Find((a) => GetPayDicId(a) == payDicId);
        return oneShopCfg;
    }


    public void CheckBuy(cfg.Game.ShopCfg cfg)
    {
        if (!GameData.Pay.TryGetStoreDisplayPriceByLocalProductId(GetPayDicId(cfg), out _))
        {
            return;
        }

        string sku = GameData.Pay.GetStoreSkuByLocalProductId(GetPayDicId(cfg));
        GPSDKMgr.Instance.TryPay(sku);
    }

    public int GetPayDicId(ShopCfg cfg)
    {
        return cfg.PayDicIdGP;
    }
}
