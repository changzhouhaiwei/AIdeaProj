using System.Collections.Generic;

/// <summary>
/// 支付结果埋点数据（支付成功/支付失败/消费成功）
/// </summary>
public class BAPaymentResultData : BaseAnaData
{
    public readonly string key = "payment_result";

    public bool success;           // 成功标记
    public float price;            // 价格
    public string currency;        // 货币
    public int quantity;           // 数量
    public string order_id;       // 订单ID
    public string server_order_id; //服务器订单ID
    public string product_id;      // 产品ID
    public string receipt_id;      // 游戏订单号
    public int http_code;         // 错误码
    public string http_message;   // 错误信息
    public string role_id;         // 角色ID
    public string role_name;       // 角色名称

    public override void ClearData()
    {
        success = false;
        price = 0;
        currency = null;
        quantity = 0;
        order_id = null;
        server_order_id = null;
        product_id = null;
        receipt_id = null;
        http_code = 0;
        http_message = null;
        role_id = null;
        role_name = null;
    }

    private Dictionary<string, object> mDic = new();
    public Dictionary<string, object> GetDict()
    {
        mDic.Clear();
        mDic["success"] = success;
        mDic["price"] = price;
        mDic["currency"] = currency;
        mDic["quantity"] = quantity;
        mDic["order_id"] = order_id;
        mDic["server_order_id"] = server_order_id;
        mDic["product_id"] = product_id;
        mDic["receipt_id"] = receipt_id;
        mDic["http_code"] = http_code;
        mDic["http_message"] = http_message;
        mDic["role_id"] = role_id;
        mDic["role_name"] = role_name;
        return mDic;
    }
}
