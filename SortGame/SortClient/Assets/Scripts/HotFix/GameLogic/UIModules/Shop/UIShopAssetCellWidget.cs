
using UnityEngine;


public class UIShopAssetCellWidget : ShopAssetCellBase
{
    private ShopData.ShopAssetData mData;

    protected override void OnOpen()
    {
        base.OnOpen();
    }

    public void UpdateData(ShopData.ShopAssetData oneData)
    {
        mData = oneData;
        icon.sprite = LoadAtlasSprite("Shop","shop_dj_" + oneData.id);
        UpdateCount();
    }

    public void UpdateCount()
    {
        numLb.text = GPUserDataManager.Instance.GetItemNumberById(mData.id).ToString();
    }
}