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

        private float _lastFireTime = -999f;

        private void Update()
        {
            if (_projectilePrefab == null) return;

            BlueEnemy target = FindClosestEnemy();
            if (target == null) return;

            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 1.2f;
            Vector3 toTarget = target.transform.position - origin;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > _range * _range) return;

            if (_rotateToTarget && toTarget.sqrMagnitude > 1e-4f)
            {
                Vector3 flatDir = toTarget.normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(flatDir, Vector3.up), 12f * Time.deltaTime);
            }

            if (Time.time - _lastFireTime < _fireInterval) return;

            Vector3 fireDir = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : transform.forward;
            BlueProjectile bullet = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(fireDir, Vector3.up));
            bullet.Launch(fireDir, _damage);

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
