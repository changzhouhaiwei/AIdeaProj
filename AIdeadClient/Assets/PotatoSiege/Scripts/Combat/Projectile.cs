using UnityEngine;

namespace PotatoSiege
{
    public class Projectile : MonoBehaviour
    {
        public Vector2 velocity;
        public float damage;
        public float life = 2.5f;
        public int pierce;
        public int bounce;
        public ElementKind element;
        public bool fromPlayer = true;
        public WeaponTag weaponTag;
        public Color color = Color.white;
        public System.Action<EnemyBrain, float> onHitEnemy;

        SpriteRenderer _sr;
        float _age;
        readonly System.Collections.Generic.HashSet<int> _hit = new System.Collections.Generic.HashSet<int>();

        public static Projectile Spawn(Vector3 pos, Vector2 dir, float speed, float dmg, Color col, ElementKind el, WeaponTag tag, int pierce = 0, int bounce = 0)
        {
            var go = new GameObject("Proj");
            go.transform.position = pos;
            var p = go.AddComponent<Projectile>();
            p.velocity = dir.normalized * speed;
            p.damage = dmg;
            p.element = el;
            p.weaponTag = tag;
            p.pierce = pierce;
            p.bounce = bounce;
            p.color = col;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.MakeCircle(col, 16);
            sr.sortingOrder = 20;
            go.transform.localScale = Vector3.one * 0.35f;
            var col2 = go.AddComponent<CircleCollider2D>();
            col2.isTrigger = true;
            col2.radius = 0.15f;
            p._sr = sr;
            return p;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age >= life)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(velocity * Time.deltaTime);

            var arena = PSConst.ArenaHalf;
            var p = transform.position;
            if (Mathf.Abs(p.x) > arena || Mathf.Abs(p.y) > arena)
            {
                if (bounce > 0)
                {
                    bounce--;
                    if (Mathf.Abs(p.x) > arena) velocity.x *= -1f;
                    if (Mathf.Abs(p.y) > arena) velocity.y *= -1f;
                    transform.position = new Vector3(Mathf.Clamp(p.x, -arena, arena), Mathf.Clamp(p.y, -arena, arena), 0);
                }
                else Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!fromPlayer) return;
            var enemy = other.GetComponent<EnemyBrain>();
            if (enemy == null) return;
            int id = enemy.GetInstanceID();
            if (_hit.Contains(id)) return;
            _hit.Add(id);

            float dmg = damage;
            var st = enemy.Status;
            if (st != null)
            {
                dmg += st.ConsumeShockBonus();
                dmg *= 1f + st.vulnerability;
            }

            enemy.TakeDamage(dmg, weaponTag, element);
            onHitEnemy?.Invoke(enemy, dmg);

            if (pierce > 0) pierce--;
            else if (bounce > 0)
            {
                bounce--;
                // redirect to nearest other
                var next = EnemyBrain.FindNearest(transform.position, enemy);
                if (next != null)
                {
                    Vector2 d = (next.transform.position - transform.position);
                    if (d.sqrMagnitude > 0.01f) velocity = d.normalized * velocity.magnitude;
                }
                else Destroy(gameObject);
            }
            else Destroy(gameObject);
        }
    }
}
