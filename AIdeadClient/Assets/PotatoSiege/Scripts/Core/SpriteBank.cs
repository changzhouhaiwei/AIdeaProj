using System.Collections.Generic;
using UnityEngine;

namespace PotatoSiege
{
    public static class SpriteBank
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Sprite _white;

        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var c = Color.white;
                    for (int i = 0; i < 16; i++) t.SetPixel(i % 4, i / 4, c);
                    t.Apply();
                    t.filterMode = FilterMode.Point;
                    _white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }
                return _white;
            }
        }

        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return White;
            if (Cache.TryGetValue(key, out var s) && s != null) return s;

            // Resources paths
            string[] paths =
            {
                $"PotatoSiege/Sprites/Player/{key}",
                $"PotatoSiege/Sprites/Enemy/{key}",
                $"PotatoSiege/Sprites/Weapon/{key}",
                $"PotatoSiege/Sprites/Tile/{key}",
                $"PotatoSiege/Sprites/{key}"
            };

            foreach (var p in paths)
            {
                var sp = Resources.Load<Sprite>(p);
                if (sp != null)
                {
                    Cache[key] = sp;
                    return sp;
                }
                var tex = Resources.Load<Texture2D>(p);
                if (tex != null)
                {
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 48f);
                    Cache[key] = sp;
                    return sp;
                }
            }

            // procedural fallback by key hash color
            s = MakeCircle(KeyColor(key), 32);
            Cache[key] = s;
            return s;
        }

        public static Sprite MakeCircle(Color color, int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f - 1f;
            Vector2 c = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                t.SetPixel(x, y, d <= r ? color : Color.clear);
            }
            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Color KeyColor(string key)
        {
            int h = key.GetHashCode();
            float r = ((h >> 16) & 255) / 255f;
            float g = ((h >> 8) & 255) / 255f;
            float b = (h & 255) / 255f;
            return new Color(Mathf.Lerp(0.3f, 1f, r), Mathf.Lerp(0.3f, 1f, g), Mathf.Lerp(0.3f, 1f, b), 1f);
        }
    }
}
