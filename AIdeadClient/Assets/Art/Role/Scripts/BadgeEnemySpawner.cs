using UnityEngine;

namespace Art.Role
{
    /// <summary>Spawns cucumber enemies from open farm map edges to chase the player.</summary>
    public class BadgeEnemySpawner : MonoBehaviour
    {
        public BadgeEnemyChase enemyPrefab;
        public Transform player;
        public float mapHalfX = 9.2f;
        public float mapHalfY = 5.1f;
        public float spawnPadding = 0.8f;
        public float intervalStart = 1.4f;
        public float intervalMin = 0.55f;
        public float intervalRamp = 0.02f;
        public int maxAlive = 28;
        public float hpRampPerSpawn = 0.03f;

        float _cd;
        float _interval;
        int _spawned;

        void Start()
        {
            _interval = intervalStart;
            _cd = 0.4f;
            if (player == null)
            {
                var p = FindObjectOfType<BadgePlayerController>();
                if (p != null) player = p.transform;
            }
        }

        void Update()
        {
            if (enemyPrefab == null || player == null) return;
            if (BadgeEnemyChase.All.Count >= maxAlive) return;

            _cd -= Time.deltaTime;
            if (_cd > 0f) return;
            _cd = _interval;
            _interval = Mathf.Max(intervalMin, _interval - intervalRamp);
            SpawnOne();
        }

        void SpawnOne()
        {
            _spawned++;
            Vector3 pos = EdgePosition();
            var enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            enemy.name = "Enemy_Cucumber_" + _spawned;
            float hpMul = 1f + _spawned * hpRampPerSpawn;
            float spdMul = 1f + Mathf.Min(0.5f, _spawned * 0.01f);
            enemy.Init(player, hpMul, spdMul);
        }

        Vector3 EdgePosition()
        {
            float ax = mapHalfX + spawnPadding;
            float ay = mapHalfY + spawnPadding;
            int side = Random.Range(0, 4);
            switch (side)
            {
                case 0: return new Vector3(Random.Range(-ax, ax), ay, 0f);   // top
                case 1: return new Vector3(Random.Range(-ax, ax), -ay, 0f);  // bottom
                case 2: return new Vector3(-ax, Random.Range(-ay, ay), 0f);  // left
                default: return new Vector3(ax, Random.Range(-ay, ay), 0f); // right
            }
        }
    }
}
