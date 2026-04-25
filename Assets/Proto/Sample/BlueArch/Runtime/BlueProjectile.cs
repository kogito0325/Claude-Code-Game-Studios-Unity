using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueProjectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 18f;
        [SerializeField] private float _lifeTime = 2.5f;
        [SerializeField] private float _hitRadius = 0.35f;
        [SerializeField] private LayerMask _enemyLayers = ~0;
        [SerializeField] private AudioClip _hitSfx;
        [SerializeField] private GameObject _hitVfxPrefab;

        private int _damage = 10;
        private Vector3 _direction;
        private float _spawnTime;

        public void Launch(Vector3 direction, int damage)
        {
            _direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward;
            _damage = Mathf.Max(0, damage);
            _spawnTime = Time.time;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= _lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            float step = _speed * Time.deltaTime;
            Vector3 next = transform.position + _direction * step;

            if (Physics.SphereCast(transform.position, _hitRadius, _direction,
                    out RaycastHit hit, step, _enemyLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<BlueEnemy>(out var enemy))
                {
                    enemy.TakeDamage(_damage);
                }
                BlueSfx.Play(_hitSfx, hit.point, 0.7f);
                if (_hitVfxPrefab != null)
                {
                    GameObject vfx = Instantiate(_hitVfxPrefab, hit.point, Quaternion.LookRotation(-_direction, Vector3.up));
                    Destroy(vfx, 0.8f);
                }
                else
                {
                    BlueVfx.Hit(hit.point);
                }
                Destroy(gameObject);
                return;
            }

            transform.position = next;
        }
    }
}
