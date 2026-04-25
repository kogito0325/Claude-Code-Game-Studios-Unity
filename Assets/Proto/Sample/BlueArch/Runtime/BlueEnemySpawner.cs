using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueEnemySpawner : MonoBehaviour
    {
        [SerializeField] private BlueEnemy _enemyPrefab;
        [SerializeField] private BlueEnemy[] _enemyVariants;
        [SerializeField] private BluePlayerController _player;

        [Header("Spawn Ring")]
        [SerializeField] private float _spawnRadius = 15f;
        [SerializeField] private float _spawnHeight = 0f;

        [Header("Rate (enemies/sec) — linearly scales over duration")]
        [SerializeField] private float _startRate = 0.8f;
        [SerializeField] private float _endRate = 4.0f;
        [SerializeField] private float _rampDuration = 180f;

        [Header("Caps")]
        [SerializeField] private int _maxAlive = 40;

        private float _elapsed;
        private float _spawnAccumulator;
        private bool _active = true;

        public void SetActive(bool active) => _active = active;

        private void Update()
        {
            if (!_active || _player == null || _player.IsDead) return;
            if (PickPrefab() == null) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(_rampDuration, 1e-3f));
            float rate = Mathf.Lerp(_startRate, _endRate, t);

            _spawnAccumulator += rate * Time.deltaTime;

            while (_spawnAccumulator >= 1f)
            {
                _spawnAccumulator -= 1f;
                if (CountAlive() >= _maxAlive) continue;
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            BlueEnemy prefab = PickPrefab();
            if (prefab == null) return;

            float angle = Random.value * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnRadius;
            Vector3 spawnPos = _player.transform.position + offset;
            spawnPos.y = _spawnHeight;

            BlueEnemy enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.Initialize(_player);
        }

        private BlueEnemy PickPrefab()
        {
            if (_enemyVariants != null && _enemyVariants.Length > 0)
            {
                int validCount = 0;
                for (int i = 0; i < _enemyVariants.Length; i++) if (_enemyVariants[i] != null) validCount++;
                if (validCount == 0) return _enemyPrefab;

                int target = Random.Range(0, validCount);
                int seen = 0;
                for (int i = 0; i < _enemyVariants.Length; i++)
                {
                    if (_enemyVariants[i] == null) continue;
                    if (seen == target) return _enemyVariants[i];
                    seen++;
                }
            }
            return _enemyPrefab;
        }

        private int CountAlive()
        {
            BlueEnemy[] all = FindObjectsByType<BlueEnemy>(FindObjectsSortMode.None);
            int alive = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !all[i].IsDead) alive++;
            }
            return alive;
        }
    }
}
