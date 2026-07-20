using UnityEngine;

namespace Art.Role
{
    /// <summary>Simple player bullet for Chapter1 badge demo.</summary>
    public class BadgeBullet : MonoBehaviour
    {
        public float speed = 14f;
        public float damage = 10f;
        public float life = 2.2f;
        public float arenaHalf = 10f;

        Vector2 _vel;
        float _age;

        public static BadgeBullet Spawn(Vector3 pos, Vector2 dir, float damage, Color color)
        {
            var go = new GameObject("BadgeBullet");
            go.transform.position = pos;
            var b = go.AddComponent<BadgeBullet>();
            b._vel = dir.sqrMagnitude > 0.001f ? dir.normalized * b.speed : Vector2.right * b.speed;
            b.damage = damage;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeDot(color);
            sr.sortingOrder = 25;
            go.transform.localScale = Vector3.one * 0.28f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            return b;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age >= life)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(_vel * Time.deltaTime);
            var p = transform.position;
            if (Mathf.Abs(p.x) > arenaHalf || Mathf.Abs(p.y) > arenaHalf)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other.GetComponent<BadgeEnemyChase>();
            if (enemy == null) return;
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }

        static Sprite MakeDot(Color c)
        {
            const int n = 16;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - r, dy = y - r;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? c : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
