using UnityEngine;
using UnityEngine.InputSystem;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueAutoAttacker : MonoBehaviour
    {
        [SerializeField] private BlueProjectile _projectilePrefab;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _fireInterval = 0.35f;
        [SerializeField] private int _damage = 10;
        [SerializeField] private InputActionReference _fireAction;
        [SerializeField] private AudioClip _fireSfx;
        [SerializeField] private GameObject _muzzleVfxPrefab;
        [SerializeField] private BlueAnimDriver _animDriver;

        private float _lastFireTime = -999f;
        private InputAction _fireActionInstance;

        private void Awake()
        {
            if (_animDriver == null) _animDriver = GetComponent<BlueAnimDriver>();
        }

        private void OnEnable()
        {
            if (_fireAction != null)
            {
                _fireActionInstance = _fireAction.action;
                _fireActionInstance.Enable();
            }
        }

        private void OnDisable()
        {
            if (_fireActionInstance != null)
            {
                _fireActionInstance.Disable();
                _fireActionInstance = null;
            }
        }

        private void Update()
        {
            if (_projectilePrefab == null) return;
            if (!IsFireHeld()) return;
            if (Time.time - _lastFireTime < _fireInterval) return;
            Fire();
        }

        private bool IsFireHeld()
        {
            if (_fireActionInstance != null) return _fireActionInstance.IsPressed();
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
        }

        private void Fire()
        {
            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 1.2f;
            Vector3 fireDir = transform.forward;
            fireDir.y = 0f;
            if (fireDir.sqrMagnitude < 1e-4f) fireDir = Vector3.forward;
            else fireDir.Normalize();

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
    }
}
