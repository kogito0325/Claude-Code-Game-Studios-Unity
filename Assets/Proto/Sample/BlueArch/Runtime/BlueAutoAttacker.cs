using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueAutoAttacker : MonoBehaviour
    {
        [SerializeField] private BlueProjectile _projectilePrefab;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _range = 10f;
        [SerializeField] private float _fireInterval = 0.35f;
        [SerializeField] private int _damage = 10;
        [SerializeField] private bool _rotateToTarget = true;
        [SerializeField] private AudioClip _fireSfx;
        [SerializeField] private GameObject _muzzleVfxPrefab;
        [SerializeField] private BlueAnimDriver _animDriver;

        private float _lastFireTime = -999f;
        private BlueEnemy _currentTarget;

        public BlueEnemy CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget != null && !_currentTarget.IsDead;

        private void Awake()
        {
            if (_animDriver == null) _animDriver = GetComponent<BlueAnimDriver>();
        }

        private void Update()
        {
            _currentTarget = null;
            if (_projectilePrefab == null) return;

            BlueEnemy target = FindClosestEnemy();
            if (target == null) return;

            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 1.2f;
            Vector3 toTarget = target.transform.position - origin;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > _range * _range) return;

            // Confirm target is in range and being tracked.
            _currentTarget = target;

            // 회전은 BluePlayerController 가 담당 (이동방향 vs 사격방향 충돌 방지).
            // _rotateToTarget 은 더 이상 사용하지 않지만 인스펙터 호환을 위해 필드 유지.

            if (Time.time - _lastFireTime < _fireInterval) return;

            Vector3 fireDir = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : transform.forward;
            BlueProjectile bullet = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(fireDir, Vector3.up));
            bullet.Launch(fireDir, _damage);

            if (_animDriver != null) _animDriver.TriggerAttack();
            BlueSfx.Play(_fireSfx, origin, 0.7f);
            if (_muzzleVfxPrefab != null)
            {
                GameObject vfx = Instantiate(_muzzleVfxPrefab, origin, Quaternion.LookRotation(fireDir, Vector3.up));
                Destroy(vfx, 0.6f);
            }
            else
            {
                BlueVfx.Muzzle(origin, fireDir);
            }

            _lastFireTime = Time.time;
        }

        private BlueEnemy FindClosestEnemy()
        {
            BlueEnemy[] all = FindObjectsByType<BlueEnemy>(FindObjectsSortMode.None);
            BlueEnemy best = null;
            float bestSq = _range * _range;

            Vector3 origin = transform.position;
            for (int i = 0; i < all.Length; i++)
            {
                BlueEnemy e = all[i];
                if (e == null || e.IsDead) continue;
                Vector3 delta = e.transform.position - origin;
                delta.y = 0f;
                float sq = delta.sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = e;
                }
            }
            return best;
        }
    }
}
