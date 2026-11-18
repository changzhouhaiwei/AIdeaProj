using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店价没拉到，或 Billing 还没 ready 时，价格位置转菊花，购买按钮不可点。两者都满足后显示价格并恢复点击。
/// </summary>
public static class ShopPriceButton
{
    const string LoadingName = "priceLoading";
    const float LoadingSize = 48f;

    public static bool Apply(TextMeshProUGUI priceText, Selectable buyBtn, int localProductId, Sprite loadingSprite)
    {
        bool ready = GameData.Pay.TryGetStoreDisplayPriceByLocalProductId(localProductId, out var price);
        ApplyVisual(priceText, buyBtn, ready ? price : "", ready, loadingSprite);
        return ready;
    }

    public static bool Apply(TextMeshProUGUI priceText, Selectable buyBtn, string storeProductId, Sprite loadingSprite)
    {
        bool ready = GameData.Pay.TryGetStoreDisplayPrice(storeProductId, out var price);
        ApplyVisual(priceText, buyBtn, ready ? price : "", ready, loadingSprite);
        return ready;
    }

    static void ApplyVisual(TextMeshProUGUI priceText, Selectable buyBtn, string price, bool ready, Sprite loadingSprite)
    {
        if (priceText == null)
        {
            return;
        }

        var loading = EnsureLoading(priceText, loadingSprite);
        priceText.text = ready ? price : "";
        priceText.gameObject.SetActive(ready);
        if (loading != null)
        {
            loading.gameObject.SetActive(!ready);
        }

        if (buyBtn == null)
        {
            return;
        }

        if (buyBtn is TButton button)
        {
            button.disableToGray = ready;
        }

        buyBtn.interactable = ready;
    }

    static RectTransform EnsureLoading(TextMeshProUGUI priceText, Sprite loadingSprite)
    {
        var parent = priceText.rectTransform.parent as RectTransform;
        if (parent == null)
        {
            return null;
        }

        var existing = parent.Find(LoadingName) as RectTransform;
        if (existing == null)
        {
            var go = new GameObject(LoadingName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ShopPriceLoading));
            existing = go.GetComponent<RectTransform>();
            existing.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = loadingSprite;
            go.SetActive(false);
        }
        else
        {
            var image = existing.GetComponent<Image>();
            if (image != null && loadingSprite != null)
            {
                image.sprite = loadingSprite;
            }
        }

        // 大礼包的价格文字是铺满按钮的，不能照抄它的锚点，否则菊花会比按钮还大。
        existing.anchorMin = new Vector2(0.5f, 0.5f);
        existing.anchorMax = new Vector2(0.5f, 0.5f);
        existing.pivot = new Vector2(0.5f, 0.5f);
        existing.anchoredPosition = Vector2.zero;
        existing.sizeDelta = new Vector2(LoadingSize, LoadingSize);
        existing.localScale = Vector3.one;
        existing.localRotation = Quaternion.identity;
        return existing;
    }
}

public class ShopPriceLoading : MonoBehaviour
{
    const float DegreesPerSecond = 360f;

    void Update()
    {
        float z = -(Time.unscaledTime * DegreesPerSecond) % 360f;
        transform.localEulerAngles = new Vector3(0f, 0f, z);
    }
}
