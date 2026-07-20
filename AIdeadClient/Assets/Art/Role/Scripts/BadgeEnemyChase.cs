using System.Collections.Generic;
using UnityEngine;

namespace Art.Role
{
    /// <summary>Cucumber (or other badge enemy): chase player from spawn edges.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BadgeEnemyChase : MonoBehaviour
    {
        public static readonly List<BadgeEnemyChase> All = new List<BadgeEnemyChase>();

        public float maxHp = 24f;
        public float moveSpeed = 2.4f;
        public float contactDamage = 6f;
        public float contactCooldown = 0.45f;
        public float touchRange = 0.55f;

        public float Hp { get; private set; }

        Transform _target;
        SpriteRenderer _sr;
        Color _baseColor = Color.white;
        float _flash;
        float _contactCd;

        public void Init(Transform target, float hpMul = 1f, float speedMul = 1f)
        {
            _target = target;
            Hp = maxHp * hpMul;
            moveSpeed *= speedMul;
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Awake()
        {
            Hp = maxHp;
            var rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Update()
        {
            if (_target == null)
            {
                var p = FindObjectOfType<BadgePlayerController>();
                if (p != null) _target = p.transform;
            }
            if (_target == null || Hp <= 0f) return;

            Vector2 dir = (Vector2)(_target.position - transform.position);
            float dist = dir.magnitude;
            if (dist > 0.01f)
            {
                transform.position += (Vector3)(dir.normalized * moveSpeed * Time.deltaTime);
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
            }

            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                if (_sr != null)
                    _sr.color = Color.Lerp(Color.white, _baseColor, 1f - Mathf.Clamp01(_flash * 6f));
            }

            _contactCd -= Time.deltaTime;
            if (dist < touchRange && _contactCd <= 0f)
            {
                _contactCd = contactCooldown;
                var player = _target.GetComponent<BadgePlayerController>();
                if (player != null) player.TakeDamage(contactDamage);
            }
        }

        public void TakeDamage(float amount)
        {
            if (Hp <= 0f) return;
            Hp -= amount;
            _flash = 0.12f;
            if (_sr != null) _sr.color = Color.red;
            if (Hp <= 0f) Destroy(gameObject);
        }

        public static BadgeEnemyChase FindNearest(Vector3 from)
        {
            BadgeEnemyChase best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var e = All[i];
                if (e == null || e.Hp <= 0f) continue;
                float d = (e.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }
    }
}
