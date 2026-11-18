using UnityEngine;
using UnityEngine.UI;
using FishFramework;

[RequireComponent(typeof(Image))]
public class BackgroundScaler : MonoBehaviour
{
    private Image backgroundImage;

    private void Start()
    {
        backgroundImage = GetComponent<Image>();
        UpdateBackgroundSize();
    }

    private void UpdateBackgroundSize()
    {
        RectTransform screenRt = GameModule.UI.UICanvas.GetComponent<RectTransform>();
        float screenWidth = screenRt.rect.width;
        float screenHeight = screenRt.rect.height;

        // 计算canvas的宽高比
        float screenRatio = screenWidth / screenHeight;

        RectTransform rt = backgroundImage.rectTransform;

        // 获取背景图的宽高比
        float bgRatio = rt.rect.width / rt.rect.height;

        if (screenRatio > bgRatio)
        {
            rt.sizeDelta = new Vector2(screenWidth, screenWidth / bgRatio);
        }
        else
        {
            rt.sizeDelta = new Vector2(screenHeight * bgRatio, screenHeight);
        }

        rt.anchoredPosition = new Vector2(0, 0);
    }
}