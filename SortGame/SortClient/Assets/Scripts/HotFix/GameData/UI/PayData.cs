using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.Utilities;
using FishFramework;
using UnityEngine;

public class PayData : BaseData
{
    public class ServerProduct
    {
        public string productId;
        public string productType;
        public string name;
        public string title;
        public string description;
        public string currency;
        public double price;
        public string formattedPrice; //实际显示的价格.

        public string ToJson()
        {
            return UnityEngine.JsonUtility.ToJson(this);
        }

        public object Clone()
        {
            return new ServerProduct
            {
                productId = this.productId,
                productType = this.productType,
                name = this.name,
                title = this.title,
                description = this.description,
                currency = this.currency,
                price = this.price,
                formattedPrice = this.formattedPrice
            };
        }
    }

    public bool QuerySuccess => GPSDKMgr.Instance.GetBPayQuerySuccess();

    private Dictionary<string, ServerProduct> m_serverProductDic = new();

    public bool IsEmpty()
    {
        return m_serverProductDic.Count == 0;
    }

    public void ParseJson(string jsonStr)
    {
        m_serverProductDic.Clear();
        try
        {
            // 使用简单的字符串处理方法解析JSON数组
            ParseJsonArray(jsonStr);

            // 测试输出
            if (m_serverProductDic.Count > 0)
            {
                var firstProduct = m_serverProductDic.Values.First();
            }
            else
            {

            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log($"pay currency exception: {e.Message}");
        }
    }

    public string GetProductString()
    {
        ServerProduct element = m_serverProductDic.Values.First();  // 随机性不保证
        return element.ToJson();
    }


    public string GetNoAdPriceText()
    {
        string key = GetNoAdsGId();
        return GetPriceByGID(key);
    }

    public string GetNoAdsGId()
    {
        string key = "soeasyXSort.noadsXSort";
#if UNITY_ANDROID
        key = "soeasyXSort.noadsXSort";
#endif
        return key;
    }

    public string GetNoAdsPayId()
    {
        string key = "11005";
#if UNITY_ANDROID
        key = "11005";
#endif
        return key;
    }

    public string GetPriceByGID(string key)
    {
        if (m_serverProductDic.Count > 0 && m_serverProductDic.TryGetValue(key, out var p))
        {
            if (!string.IsNullOrEmpty(p.formattedPrice))
            {
                return p.formattedPrice;
            }
        }

        var localPrice = GPSDKMgr.Instance.GetLocalPrice(key, true);
        if (!string.IsNullOrEmpty(localPrice) && localPrice != "??")
        {
            return localPrice;
        }

        // 埋点和旧调用仍可用配置价。购买按钮展示不走这里，见 TryGetStoreDisplayPrice。
        return GetFallbackPriceByGID(key);
    }

    /// <summary>
    /// 商店价是否已经拉到，且 Billing 已经 ready。编辑器用配置价，方便直接点购买。
    /// 真机只有商店格式化价或 Billing 本地价算拉到；Billing 未 ready 时同样不算可购买，按钮保持转圈且不可点。
    /// </summary>
    public bool TryGetStoreDisplayPrice(string key, out string price)
    {
#if UNITY_EDITOR
        price = GetFallbackPriceByGID(key);
        return !string.IsNullOrEmpty(price) && price != "??";
#else
        price = "";
        if (GPSDKMgr.Instance == null || !GPSDKMgr.Instance.IsBillingReady())
        {
            return false;
        }

        if (!string.IsNullOrEmpty(key)
            && m_serverProductDic.Count > 0
            && m_serverProductDic.TryGetValue(key, out var product)
            && !string.IsNullOrEmpty(product.formattedPrice))
        {
            price = product.formattedPrice;
            return true;
        }

        if (!string.IsNullOrEmpty(key))
        {
            var localPrice = GPSDKMgr.Instance.GetLocalPrice(key, true);
            if (!string.IsNullOrEmpty(localPrice) && localPrice != "??")
            {
                price = localPrice;
                return true;
            }
        }

        return false;
#endif
    }

    public bool TryGetStoreDisplayPriceByLocalProductId(int localPId, out string price)
    {
        var cfg = ConfigHelper.Tables.TbPayProductCfg.GetOrDefault(localPId);
        return TryGetStoreDisplayPrice(GetProductIdByPlatform(cfg), out price);
    }

    private string GetStoreProductId(cfg.Game.PayProductCfg cfg)
    {
        if (cfg == null)
        {
            return "";
        }

#if UNITY_IOS && !UNITY_EDITOR
        return cfg.ProductIdIos ?? "";
#else
        return cfg.ProductIdGoogle ?? "";
#endif
    }

    public bool MatchesStoreProductId(cfg.Game.PayProductCfg cfg, string storeProdId)
    {
        return GetStoreProductId(cfg) == storeProdId;
    }

    private string GetFallbackPriceByGID(string key)
    {
        var list = ConfigHelper.Tables.TbPayProductCfg.DataList;
        foreach (var item in list)
        {
            if (!MatchesStoreProductId(item, key))
            {
                continue;
            }

            var priceText = item.ProductPrice.ToString("0.##");
            if (item.Currency == "USD")
            {
                return "$" + priceText;
            }
            return item.Currency + " " + priceText;
        }

        return "??";
    }

    public string GetPriceByLocalProductId(int localPId)
    {
        var d = ConfigHelper.Tables.TbPayProductCfg.GetOrDefault(localPId);
        string productId = GetProductIdByPlatform(d);
        return GetPriceByGID(productId);
    }

    public string GetProductStringByLocalProductId(int localPid)
    {
        var d = ConfigHelper.Tables.TbPayProductCfg.GetOrDefault(localPid);
        string productId = GetProductIdByPlatform(d);
        return GetProductStringByGID(productId);
    }

    /// <summary>商店 SKU（Google Play product id），供 GPSDK 支付使用。</summary>
    public string GetStoreSkuByLocalProductId(int localPId)
    {
        var d = ConfigHelper.Tables.TbPayProductCfg.GetOrDefault(localPId);
        return d != null ? GetProductIdByPlatform(d) : "";
    }

    /// <summary>根据商店商品 id 查配置（与 Mahjong PayData 一致，供支付埋点）。</summary>
    public cfg.Game.PayProductCfg GetPayCfgByStoreProdId(string storeProdId)
    {
        var list = ConfigHelper.Tables.TbPayProductCfg.DataList;
        foreach (var item in list)
        {
            if (MatchesStoreProductId(item, storeProdId))
            {
                return item;
            }
        }
        return null;
    }

    private string GetProductIdByPlatform(cfg.Game.PayProductCfg cfg)
    {
        return GetStoreProductId(cfg);
    }

    public string GetProductStringByGID(string googleProdId)
    {
        string key = googleProdId;
        cfg.Game.PayProductCfg localCfg = null;
        var dataList = ConfigHelper.Tables.TbPayProductCfg.DataList;

        foreach (var item in dataList)
        {
            if (MatchesStoreProductId(item, googleProdId))
            {
                localCfg = item;
                break;
            }
        }

        if (localCfg != null)
        {
            if (m_serverProductDic.ContainsKey(key))
            {
                var sData = m_serverProductDic[key];
                ServerProduct retData = sData.Clone() as ServerProduct;
                // 设置对应平台的 product_id
                retData.productId = localCfg.Id.ToString();//GetProductIdByPlatform(localCfg);

                int priceI = (int)System.Math.Round(localCfg.ProductPrice * 100, System.MidpointRounding.AwayFromZero);
                double priced = priceI;
                retData.price = priced;
                retData.name = localCfg.ProductName;
                retData.currency = localCfg.Currency;
                return retData.ToJson();
            }
        }
        return "";
    }
    public string GetNoAdProductString()//获得无广告产品的配置
    {
        string key = GetNoAdsGId();
        return GetProductStringByGID(key);
    }

    public int ConvertPriceToCent(float price)//美元转没分
    {
        int priceI = (int)System.Math.Round(price * 100, System.MidpointRounding.AwayFromZero);
        return priceI;
    }

    /// <summary>
    /// 解析JSON数组的简单方法
    /// </summary>
    private void ParseJsonArray(string jsonStr)
    {
        // 移除开头和结尾的方括号
        string trimmedJson = jsonStr.Trim();
        if (trimmedJson.StartsWith("[") && trimmedJson.EndsWith("]"))
        {
            trimmedJson = trimmedJson.Substring(1, trimmedJson.Length - 2);
        }

        // 分割JSON对象
        var jsonObjects = SplitJsonObjects(trimmedJson);

        foreach (var objJson in jsonObjects)
        {
            if (!string.IsNullOrEmpty(objJson))
            {
                var product = ParseSingleProduct(objJson);
                if (product != null && !string.IsNullOrEmpty(product.productId))
                {
                    m_serverProductDic[product.productId] = product;
                }
            }
        }
    }

    /// <summary>
    /// 解析单个产品对象
    /// </summary>
    private ServerProduct ParseSingleProduct(string jsonStr)
    {
        ServerProduct product = null;
        try
        {
            // 使用Unity的JsonUtility解析单个对象
            product = UnityEngine.JsonUtility.FromJson<ServerProduct>(jsonStr);
        }
        catch
        {
            // 如果JsonUtility失败，尝试手动解析
            product = ParseProductManually(jsonStr);
        }

        // 如果解析成功，需要根据平台选择 product_id
        if (product != null)
        {
            SelectProductIdByPlatform(product, jsonStr);
        }

        return product;
    }

    /// <summary>
    /// 根据平台选择对应的 product_id
    /// </summary>
    private void SelectProductIdByPlatform(ServerProduct product, string jsonStr)
    {
        // 如果 productId 已经存在（旧格式），直接返回
        if (!string.IsNullOrEmpty(product.productId))
        {
            return;
        }

        var pairs = ExtractKeyValuePairs(jsonStr);
        string productIdGoogle = null;
        string productIdIos = null;

        foreach (var pair in pairs)
        {
            switch (pair.Key.ToLower())
            {
                case "product_id_google":
                    productIdGoogle = pair.Value;
                    break;
                case "product_id_ios":
                    productIdIos = pair.Value;
                    break;
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        product.productId = productIdIos ?? productIdGoogle ?? "";
#else
        product.productId = productIdGoogle ?? productIdIos ?? "";
#endif
    }

    /// <summary>
    /// 手动解析产品对象（备用方法）
    /// </summary>
    private ServerProduct ParseProductManually(string jsonStr)
    {
        var product = new ServerProduct();

        // 简单的键值对解析
        var pairs = ExtractKeyValuePairs(jsonStr);
        string productIdGoogle = null;
        string productIdIos = null;

        foreach (var pair in pairs)
        {
            switch (pair.Key.ToLower())
            {
                case "product_id_google":
                    productIdGoogle = pair.Value;
                    break;
                case "product_id_ios":
                    productIdIos = pair.Value;
                    break;
                case "productid": // 兼容旧格式
                    product.productId = pair.Value;
                    break;
                case "producttype":
                    product.productType = pair.Value;
                    break;
                case "name":
                    product.name = pair.Value;
                    break;
                case "title":
                    product.title = pair.Value;
                    break;
                case "description":
                    product.description = pair.Value;
                    break;
                case "currency":
                    product.currency = pair.Value;
                    break;
                case "price":
                    if (double.TryParse(pair.Value, out double price))
                        product.price = price;
                    break;
                case "formattedprice":
                    product.formattedPrice = pair.Value;
                    break;
            }
        }

        if (string.IsNullOrEmpty(product.productId))
        {
#if UNITY_IOS && !UNITY_EDITOR
            product.productId = productIdIos ?? productIdGoogle ?? "";
#else
            product.productId = productIdGoogle ?? productIdIos ?? "";
#endif
        }

        return product;
    }

    /// <summary>
    /// 提取JSON中的键值对
    /// </summary>
    private Dictionary<string, string> ExtractKeyValuePairs(string jsonStr)
    {
        var result = new Dictionary<string, string>();

        // 移除大括号
        string content = jsonStr.Trim();
        if (content.StartsWith("{") && content.EndsWith("}"))
        {
            content = content.Substring(1, content.Length - 2);
        }

        // 分割键值对
        var pairs = content.Split(',');
        foreach (var pair in pairs)
        {
            var colonIndex = pair.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = pair.Substring(0, colonIndex).Trim().Trim('"');
                var value = pair.Substring(colonIndex + 1).Trim().Trim('"');
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// 分割JSON对象
    /// </summary>
    private List<string> SplitJsonObjects(string jsonStr)
    {
        var result = new List<string>();
        var current = "";
        var braceCount = 0;
        var inString = false;
        var escapeNext = false;

        for (int i = 0; i < jsonStr.Length; i++)
        {
            char c = jsonStr[i];

            if (escapeNext)
            {
                current += c;
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                current += c;
                continue;
            }

            if (c == '"' && !escapeNext)
            {
                inString = !inString;
            }

            if (!inString)
            {
                if (c == '{')
                {
                    braceCount++;
                }
                else if (c == '}')
                {
                    braceCount--;
                }
                else if (c == ',' && braceCount == 0)
                {
                    if (!string.IsNullOrEmpty(current.Trim()))
                    {
                        result.Add(current.Trim());
                    }
                    current = "";
                    continue;
                }
            }

            current += c;
        }

        if (!string.IsNullOrEmpty(current.Trim()))
        {
            result.Add(current.Trim());
        }

        return result;
    }

    /// <summary>
    /// 获取所有产品
    /// </summary>
    public List<ServerProduct> GetAllProducts()
    {
        return m_serverProductDic.Values.ToList();
    }

    /// <summary>
    /// 检查是否有免广告产品
    /// </summary>
    public bool HasNoAdsProduct()
    {
        return m_serverProductDic.ContainsKey(GetNoAdsGId());
    }

    public override void OnInit()
    {
    }

    public override void OnReset()
    {
        m_serverProductDic.Clear();
    }

    public override void OnRemove()
    {
        m_serverProductDic.Clear();
    }

    public void CheckRestorePay()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (GPUserDataManager.Instance.GetBNoAd())
        {
            AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UI_SettingPanel_RestoreOderTips2"));
            return;
        }

        if (!QuerySuccess)
        {
            GPSDKMgr.Instance.QueryCurrencyInfo();
            AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UI_SettingPanel_RestoreOderTips3"));
            return;
        }

        MainBehaviour.Instance.StartCoroutine(RestorePayCoroutine());
#endif
    }

    IEnumerator RestorePayCoroutine()
    {
#if UNITY_IOS && !UNITY_EDITOR
        GameModule.UI.SetBLoading(true);
        yield return GPSDKMgr.Instance.WaitBillingRestoreIdle();
        GPSDKMgr.Instance.BeginRestorePayWait();
        GPSDKMgr.Instance.ReqManualRestoreNoAdPurchase();
        yield return GPSDKMgr.Instance.WaitRestorePayComplete(12f);
        GameModule.UI.SetBLoading(false);

        if (GPSDKMgr.Instance.GetRestorePaySucceeded()
            && GPSDKMgr.Instance.TryGrantManualRestoredNoAd())
        {
            yield break;
        }

        AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UI_SettingPanel_RestoreOderTips2"));
#else
        yield break;
#endif
    }
}