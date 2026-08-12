using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
class CharLayoutFile
{
    public float fade_in = 1.4f;
    public float hold = 30.0f;
    public float fade_out = 1.4f;
    public float gap = 0.6f;
    public CharLayoutEntry[] characters;
}

[Serializable]
class CharLayoutEntry
{
    public string file;
    public float scale = 0.32f;
    public float[] anchor;
    public float[] hip;
}

/// <summary>
/// Standing passengers (window-facing, center): fade-in / hold / fade-out,
/// random pick each round (avoid immediate repeat). Port of AIdeaDesktop/src/chars.py.
/// </summary>
public class CharacterCycle
{
    struct SpriteItem
    {
        public SpriteRenderer renderer;
    }

    readonly List<SpriteItem> _items = new List<SpriteItem>();
    readonly float _fadeIn;
    readonly float _hold;
    readonly float _fadeOut;
    readonly float _gap;
    readonly float _cycleLen;
    float _t;
    int _index;

    public bool Enabled => _items.Count > 0;

    public CharacterCycle(
        Transform parent,
        int screenW,
        int screenH,
        int fgNativeW,
        int fgNativeH,
        TextAsset layoutAsset,
        Sprite[] inspectorSprites)
    {
        var layout = new CharLayoutFile();
        var asset = layoutAsset != null
            ? layoutAsset
            : Resources.Load<TextAsset>("Wallpaper/Chars/layout");
        if (asset != null)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(asset.text, layout);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CharacterCycle] layout: {e.Message}");
            }
        }

        _fadeIn = layout.fade_in;
        _hold = layout.hold;
        _fadeOut = layout.fade_out;
        _gap = layout.gap;
        _cycleLen = _fadeIn + _hold + _fadeOut + _gap;

        if (layout.characters == null || layout.characters.Length == 0)
            return;

        // Optional inspector overrides keyed by file name (without extension).
        var byName = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        if (inspectorSprites != null)
        {
            foreach (var s in inspectorSprites)
            {
                if (s != null)
                    byName[s.name] = s;
            }
        }

        foreach (var entry in layout.characters)
        {
            if (entry == null || string.IsNullOrEmpty(entry.file))
                continue;

            string key = System.IO.Path.GetFileNameWithoutExtension(entry.file);
            Sprite spr = null;
            if (!byName.TryGetValue(key, out spr) || spr == null)
                spr = Resources.Load<Sprite>($"Wallpaper/Chars/{key}");
            if (spr == null)
            {
                Debug.LogWarning($"[CharacterCycle] missing sprite: {key}");
                continue;
            }

            float ax = entry.anchor != null && entry.anchor.Length >= 2 ? entry.anchor[0] : 0f;
            float ay = entry.anchor != null && entry.anchor.Length >= 2 ? entry.anchor[1] : 0f;
            float hipX = entry.hip != null && entry.hip.Length >= 2 ? entry.hip[0] : 0.5f;
            float hipY = entry.hip != null && entry.hip.Length >= 2 ? entry.hip[1] : 0.58f;

            CoverMap(0, 0, fgNativeW, fgNativeH, screenW, screenH, out _, out _, out float coverS);
            float scale = entry.scale * coverS;
            float texW = spr.rect.width;
            float texH = spr.rect.height;
            int nw = Mathf.Max(1, Mathf.RoundToInt(texW * scale));
            int nh = Mathf.Max(1, Mathf.RoundToInt(texH * scale));

            CoverMap(ax, ay, fgNativeW, fgNativeH, screenW, screenH, out float sax, out float say, out _);
            float cx = nw * hipX;
            float cy = nh * hipY;
            float topLeftX = sax - cx;
            float topLeftYFromTop = say - cy;

            var go = new GameObject(key);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = 20;
            // Place by top-left corner in screen/pixel space (Y flipped vs pygame).
            float ppu = spr.pixelsPerUnit;
            go.transform.localScale = new Vector3(nw / texW, nh / texH, 1f);
            // With default center pivot sprites, convert top-left to center position.
            float pivotX = spr.pivot.x / texW;
            float pivotY = spr.pivot.y / texH;
            float centerFromTopLeftX = nw * pivotX;
            float centerFromTopLeftY = nh * (1f - pivotY);
            var pos = new Vector3(
                topLeftX + centerFromTopLeftX,
                screenH - (topLeftYFromTop + centerFromTopLeftY),
                0f);
            go.transform.position = pos;
            sr.color = new Color(1f, 1f, 1f, 0f);

            _items.Add(new SpriteItem { renderer = sr });
            Debug.Log($"[CharacterCycle] {key} -> {nw}x{nh} at {pos}");
        }

        _index = PickRandom(-1);
        ApplyAlpha(0f);
    }

    static void CoverMap(
        float x, float y, int srcW, int srcH, int dstW, int dstH,
        out float sx, out float sy, out float scale)
    {
        scale = Mathf.Max(dstW / (float)srcW, dstH / (float)srcH);
        float nw = srcW * scale;
        float nh = srcH * scale;
        float ox = (nw - dstW) / 2f;
        float oy = (nh - dstH) / 2f;
        sx = x * scale - ox;
        sy = y * scale - oy;
    }

    int PickRandom(int avoid)
    {
        int n = _items.Count;
        if (n <= 0) return 0;
        if (n == 1) return 0;
        int pick;
        do { pick = UnityEngine.Random.Range(0, n); }
        while (pick == avoid && n > 1);
        return pick;
    }

    public void Update(float dt, bool paused)
    {
        if (!Enabled || paused || _cycleLen <= 0f) return;
        _t += dt;
        while (_t >= _cycleLen)
        {
            _t -= _cycleLen;
            _index = PickRandom(_index);
        }
        ApplyAlpha(CurrentAlpha());
    }

    float CurrentAlpha()
    {
        float t = _t;
        if (t < _fadeIn)
            return _fadeIn > 0f ? t / _fadeIn : 1f;
        t -= _fadeIn;
        if (t < _hold)
            return 1f;
        t -= _hold;
        if (t < _fadeOut)
            return _fadeOut > 0f ? 1f - (t / _fadeOut) : 0f;
        return 0f;
    }

    void ApplyAlpha(float a)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var c = _items[i].renderer.color;
            c.a = (i == _index) ? a : 0f;
            _items[i].renderer.color = c;
        }
    }
}
