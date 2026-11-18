using System.Collections.Generic;

/// <summary>
/// 创建订单埋点数据
/// </summary>
public class BACreateOrderData : BaseAnaData
{
    public readonly string key = "create_order";

    public float price;           // 价格
    public string currency;        // 货币（转换为USD）
    public int quantity;           // 数量
    public string order_id;        // 订单ID
    public string product_id;       // 产品ID
    public string receipt_id;      // 游戏订单号
    public string role_id;         // 角色ID
    public string role_name;       // 角色名称

    public override void ClearData()
    {
        price = 0;
        currency = null;
        quantity = 0;
        order_id = null;
        product_id = null;
        receipt_id = null;
        role_id = null;
        role_name = null;
    }

    private Dictionary<string, object> mDic = new();
    public Dictionary<string, object> GetDict()
    {
        mDic.Clear();
        mDic["price"] = price;
        mDic["currency"] = currency;
        mDic["quantity"] = quantity;
        mDic["order_id"] = order_id;
        mDic["product_id"] = product_id;
        mDic["receipt_id"] = receipt_id;
        mDic["role_id"] = role_id;
        mDic["role_name"] = role_name;
        return mDic;
    }
}
