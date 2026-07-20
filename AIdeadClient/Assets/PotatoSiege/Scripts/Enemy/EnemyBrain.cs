using System.Collections.Generic;
using UnityEngine;

namespace PotatoSiege
{
    public class EnemyBrain : MonoBehaviour
    {
        public static readonly List<EnemyBrain> All = new List<EnemyBrain>();

        public float maxHp = 20f;
        public float hp = 20f;
        public float contactDamage = 8f;
        public float moveSpeed = 2.2f;
        public float SlowMul = 1f;
        public bool isElite;
        public bool isBoss;
        public int goldReward = 2;
        public int xpReward = 3;

        public StatusHost Status { get; private set; }

        Transform _player;
        SpriteRenderer _sr;
        float _hitFlash;
        float _dotAcc;

        public static EnemyBrain FindNearest(Vector3 from, EnemyBrain exclude = null)
        {
            EnemyBrain best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var e = All[i];
                if (e == null || e == exclude || e.hp <= 0) continue;
                float d = (e.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        public static void GetNearestN(Vector3 from, int n, List<EnemyBrain> buffer)
        {
            buffer.Clear();
            var tmp = new List<(EnemyBrain e, float d)>();
            foreach (var e in All)
            {
                if (e == null || e.hp <= 0) continue;
                tmp.Add((e, (e.transform.position - from).sqrMagnitude));
            }
            tmp.Sort((a, b) => a.d.CompareTo(b.d));
            for (int i = 0; i < tmp.Count && i < n; i++) buffer.Add(tmp[i].e);
        }

        public void Setup(float hpMul, float dmgMul, bool elite, bool boss, Transform player)
        {
            _player = player;
            isElite = elite;
            isBoss = boss;
            maxHp = 20f * hpMul * (elite ? 4f : 1f) * (boss ? 18f : 1f);
            hp = maxHp;
            contactDamage = 8f * dmgMul * (elite ? 1.5f : 1f) * (boss ? 2.2f : 1f);
            moveSpeed = (elite ? 2.6f : 2.2f) * (boss ? 1.4f : 1f);
            goldReward = elite ? 25 : boss ? 80 : 2;
            xpReward = elite ? 20 : boss ? 60 : 3;
            Status = gameObject.AddComponent<StatusHost>();
            Status.InitEnemy(this);

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = SpriteBank.Get("zombie");
            _sr.color = boss ? new Color(1f, 0.3f, 0.2f) : elite ? new Color(1f, 0.7f, 0.2f) : Color.white;
            _sr.sortingOrder = 10;
            float scale = boss ? 2.2f : elite ? 1.35f : 1f;
            transform.localScale = Vector3.one * scale;

            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (GameSession.I == null || GameSession.I.Phase != GamePhase.Wave) return;
            if (_player == null || hp <= 0) return;

            Vector2 dir = (_player.position - transform.position);
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.position += (Vector3)(dir.normalized * moveSpeed * SlowMul * Time.deltaTime);
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, ang);
            }

            // clamp arena
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -PSConst.ArenaHalf, PSConst.ArenaHalf);
            p.y = Mathf.Clamp(p.y, -PSConst.ArenaHalf, PSConst.ArenaHalf);
            transform.position = p;

            if (_hitFlash > 0)
            {
                _hitFlash -= Time.deltaTime;
                if (_sr != null) _sr.color = Color.Lerp(_sr.color, Color.white, 1f - Mathf.Clamp01(_hitFlash * 5f));
            }

            // contact damage
            if (dir.magnitude < 0.7f * transform.localScale.x)
            {
                GameSession.I.Player.TryContactHit(contactDamage * Time.deltaTime * 2.5f);
            }
        }

        public void ApplyDot(float amount)
        {
            _dotAcc += amount;
            if (_dotAcc >= 1f)
            {
                float whole = Mathf.Floor(_dotAcc);
                _dotAcc -= whole;
                TakeDamage(whole, WeaponTag.Element, ElementKind.None, true);
            }
        }

        public void TakeDamage(float amount, WeaponTag tag, ElementKind element, bool isDot = false)
        {
            if (hp <= 0) return;
            hp -= amount;
            _hitFlash = 0.15f;
            if (_sr != null) _sr.color = Color.red;

            if (!isDot && element != ElementKind.None && Status != null)
            {
                bool dao = GameSession.I != null && GameSession.I.ClassId == ClassId.Daoist;
                Status.AddElement(element, 1f, dao);
            }

            if (hp <= 0) Die();
        }

        void Die()
        {
            if (GameSession.I != null)
            {
                int gold = goldReward;
                if (!isElite && !isBoss)
                    gold = Mathf.Max(1, Mathf.RoundToInt(2f * Mathf.Lerp(0.8f, 2f, (GameSession.I.Wave - 1) / 19f)));
                GameSession.I.AddKillReward(gold, xpReward, transform.position);
            }

            // punch inheritance
            if (Status != null && Status.punchStacks > 0)
            {
                var nearest = FindNearest(transform.position, this);
                if (nearest != null && nearest.Status != null)
                {
                    int inherit = Status.punchStacks / 2;
                    for (int i = 0; i < inherit; i++) nearest.Status.AddPunchStack();
                }
            }

            Destroy(gameObject);
        }
    }
}
