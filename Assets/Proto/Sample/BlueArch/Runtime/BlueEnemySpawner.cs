using System.Collections.Generic;
using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueEnemySpawner : MonoBehaviour
    {
        public enum PickMode
        {
            Random,
            RoundRobin
        }

        [SerializeField] private BlueEnemy _enemyPrefab;
        [SerializeField] private BlueEnemy[] _enemyVariants;
        [SerializeField] private BluePlayerController _player;

        [Header("Rate (enemies/sec) — linearly scales over duration")]
        [SerializeField] private float _startRate = 0.8f;
        [SerializeField] private float _endRate = 4.0f;
        [SerializeField] private float _rampDuration = 180f;

        [Header("Caps")]
        [SerializeField] private int _maxAlive = 40;

        [Header("Spawn Points")]
        [Tooltip("선택 모드. Random=무작위, RoundRobin=순서대로.")]
        [SerializeField] private PickMode _pickMode = PickMode.Random;

        [Tooltip("플레이어로부터 이 거리보다 가까운 SpawnPoint는 제외. 0 = 비활성.")]
        [SerializeField, Min(0f)] private float _minDistanceFromPlayer = 0f;

        [Tooltip("플레이어로부터 이 거리보다 먼 SpawnPoint는 제외. 0 = 비활성.")]
        [SerializeField, Min(0f)] private float _maxDistanceFromPlayer = 0f;

        private float _elapsed;
        private float _spawnAccumulator;
        private bool _active = true;
        private int _roundRobinCursor;
        private bool _warnedNoPoints;

        private static readonly List<BlueEnemySpawnPoint> _candidateBuffer = new List<BlueEnemySpawnPoint>(32);

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

            BlueEnemySpawnPoint point = PickSpawnPoint();
            if (point == null)
            {
                if (!_warnedNoPoints)
                {
                    _warnedNoPoints = true;
                    Debug.LogWarning("[BlueEnemySpawner] 사용 가능한 BlueEnemySpawnPoint가 없습니다. 씬에 스폰 포인트를 배치하세요.", this);
                }
                return;
            }

            Vector3 spawnPos = point.GetSpawnPosition();
            BlueEnemy enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.Initialize(_player);
        }

        private BlueEnemySpawnPoint PickSpawnPoint()
        {
            IReadOnlyList<BlueEnemySpawnPoint> all = BlueEnemySpawnPoint.All;
            if (all == null || all.Count == 0) return null;

            _candidateBuffer.Clear();
            Vector3 playerPos = _player != null ? _player.transform.position : Vector3.zero;
            bool hasMin = _minDistanceFromPlayer > 0f;
            bool hasMax = _maxDistanceFromPlayer > 0f;
            float minSqr = _minDistanceFromPlayer * _minDistanceFromPlayer;
            float maxSqr = _maxDistanceFromPlayer * _maxDistanceFromPlayer;

            for (int i = 0; i < all.Count; i++)
            {
                BlueEnemySpawnPoint p = all[i];
                if (p == null) continue;
                if (hasMin || hasMax)
                {
                    Vector3 d = p.transform.position - playerPos;
                    d.y = 0f;
                    float sqr = d.sqrMagnitude;
                    if (hasMin && sqr < minSqr) continue;
                    if (hasMax && sqr > maxSqr) continue;
                }
                _candidateBuffer.Add(p);
            }

            if (_candidateBuffer.Count == 0) return null;

            switch (_pickMode)
            {
                case PickMode.RoundRobin:
                {
                    int idx = _roundRobinCursor % _candidateBuffer.Count;
                    _roundRobinCursor = (_roundRobinCursor + 1) % _candidateBuffer.Count;
                    return _candidateBuffer[idx];
                }
                default:
                    return _candidateBuffer[Random.Range(0, _candidateBuffer.Count)];
            }
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
