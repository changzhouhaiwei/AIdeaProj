using UnityEngine;

namespace PotatoSiege
{
    public class WaveDirector : MonoBehaviour
    {
        GameSession _session;
        Transform _player;
        float _spawnAcc;
        float _spawnInterval = 1.2f;
        int _spawned;
        int _spawnBudget;

        public void Init(GameSession s) => _session = s;

        public void StartWave(int wave, Transform player)
        {
            _player = player;
            _spawnAcc = 0;
            _spawned = 0;
            float countMul = CombatMath.WaveCountMul(wave);
            _spawnBudget = Mathf.RoundToInt(18 * countMul);
            _spawnInterval = Mathf.Max(0.35f, 1.3f - wave * 0.03f);

            if (wave == 10)
            {
                SpawnEnemy(true, false);
                SpawnEnemy(true, false);
            }
            if (wave == 15) SpawnEnemy(false, true);
            if (wave == 20) SpawnEnemy(false, true);
        }

        public void Tick(float dt)
        {
            if (_session.Phase != GamePhase.Wave) return;
            float t = _session.WaveTimer;
            // last 10s reduce spawn 20%
            float rateMul = t < 10f ? 0.8f : 1f;
            _spawnAcc += dt * rateMul;
            while (_spawnAcc >= _spawnInterval && _spawned < _spawnBudget)
            {
                _spawnAcc -= _spawnInterval;
                SpawnEnemy(false, false);
            }
        }

        void SpawnEnemy(bool elite, bool boss)
        {
            if (_player == null) return;
            _spawned++;
            var go = new GameObject(boss ? "Boss" : elite ? "Elite" : "Enemy");
            go.transform.SetParent(transform, false);
            Vector2 pos = RandomEdge();
            go.transform.position = pos;
            var e = go.AddComponent<EnemyBrain>();
            int wave = _session.Wave;
            e.Setup(CombatMath.WaveHpMul(wave), CombatMath.WaveDmgMul(wave), elite, boss, _player);
        }

        Vector2 RandomEdge()
        {
            float a = PSConst.ArenaHalf + 0.5f;
            int side = Random.Range(0, 4);
            return side switch
            {
                0 => new Vector2(Random.Range(-a, a), a),
                1 => new Vector2(Random.Range(-a, a), -a),
                2 => new Vector2(a, Random.Range(-a, a)),
                _ => new Vector2(-a, Random.Range(-a, a)),
            };
        }
    }
}
