using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class MinCell : MonoBehaviour
{
    Image _img;

    void Awake()
    {
        _img = GetComponent<Image>();
        EnsureSprite();
        _img.raycastTarget = false;
    }

    void EnsureSprite()
    {
        if (_img.sprite != null) return;
        var tex = Texture2D.whiteTexture;
        _img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    public void SetVisual(Color color, bool filled)
    {
        if (_img == null) _img = GetComponent<Image>();
        EnsureSprite();
        _img.color = color;
        _img.enabled = true;
        _img.raycastTarget = false;
    }
}
