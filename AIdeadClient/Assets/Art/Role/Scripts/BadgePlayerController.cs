using UnityEngine;

namespace Art.Role
{
    /// <summary>
    /// Chapter1 player on Role_Potato prefab: WASD move, mouse aim, auto/manual fire.
    /// Rotates Aim_L / Aim_R and nudges eyes toward aim.
    /// Open farm map — rectangular bounds, not a closed arena.
    /// </summary>
    public class BadgePlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        [Tooltip("Half-width of playable farm (world units).")]
        public float mapHalfX = 9.2f;
        [Tooltip("Half-height of playable farm (world units).")]
        public float mapHalfY = 5.1f;
        public float fireInterval = 0.22f;
        public float bulletDamage = 12f;
        public float eyeAimOffset = 0.08f;
        public Color bulletColor = new Color(1f, 0.85f, 0.2f, 1f);

        public float maxHp = 100f;
        public float Hp { get; private set; }

        Transform _aimL;
        Transform _aimR;
        Transform _eyeL;
        Transform _eyeR;
        Vector3 _eyeLRest;
        Vector3 _eyeRRest;
        float _fireCd;
        Camera _cam;

        void Awake()
        {
            Hp = maxHp;
            _cam = Camera.main;
            CacheVisuals();
            EnsurePhysics();
        }

        void CacheVisuals()
        {
            _aimL = transform.Find("Visual/Aim_L");
            _aimR = transform.Find("Visual/Aim_R");
            _eyeL = transform.Find("Visual/Eye_L");
            _eyeR = transform.Find("Visual/Eye_R");
            if (_eyeL != null) _eyeLRest = _eyeL.localPosition;
            if (_eyeR != null) _eyeRRest = _eyeR.localPosition;
        }

        void EnsurePhysics()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = GetComponent<CircleCollider2D>();
            if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
        }

        void Update()
        {
            HandleMove();
            Vector2 aim = GetAimDir();
            ApplyAimVisual(aim);
            HandleFire(aim);
        }

        void HandleMove()
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            if (input.sqrMagnitude > 1f) input.Normalize();

            transform.position += (Vector3)(input * moveSpeed * Time.deltaTime);
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -mapHalfX, mapHalfX);
            p.y = Mathf.Clamp(p.y, -mapHalfY, mapHalfY);
            transform.position = p;
        }

        Vector2 GetAimDir()
        {
            if (_cam == null) _cam = Camera.main;
            var nearest = BadgeEnemyChase.FindNearest(transform.position);
            if (nearest != null)
            {
                Vector2 to = nearest.transform.position - transform.position;
                if (to.sqrMagnitude > 0.01f) return to.normalized;
            }

            if (_cam != null)
            {
                Vector3 mp = _cam.ScreenToWorldPoint(Input.mousePosition);
                mp.z = 0f;
                Vector2 to = mp - transform.position;
                if (to.sqrMagnitude > 0.01f) return to.normalized;
            }
            return Vector2.right;
        }

        void ApplyAimVisual(Vector2 aim)
        {
            float ang = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            if (_aimL != null) _aimL.localRotation = Quaternion.Euler(0f, 0f, ang - 135f);
            if (_aimR != null) _aimR.localRotation = Quaternion.Euler(0f, 0f, ang - 45f);

            Vector3 nudge = (Vector3)(aim * eyeAimOffset);
            if (_eyeL != null) _eyeL.localPosition = _eyeLRest + nudge;
            if (_eyeR != null) _eyeR.localPosition = _eyeRRest + nudge;
        }

        void HandleFire(Vector2 aim)
        {
            _fireCd -= Time.deltaTime;
            bool want = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || BadgeEnemyChase.All.Count > 0;
            if (!want || _fireCd > 0f) return;
            _fireCd = fireInterval;

            Vector3 muzzle = transform.position + (Vector3)(aim * 0.55f);
            BadgeBullet.Spawn(muzzle, aim, bulletDamage, bulletColor);
        }

        public void TakeDamage(float amount)
        {
            Hp = Mathf.Max(0f, Hp - amount);
            if (Hp <= 0f)
                Debug.Log("[Chapter1] Potato down — press Play again to retry.");
        }
    }
}
