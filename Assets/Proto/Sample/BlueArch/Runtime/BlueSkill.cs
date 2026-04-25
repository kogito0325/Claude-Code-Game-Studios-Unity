using UnityEngine;
using UnityEngine.InputSystem;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueSkill : MonoBehaviour
    {
        [SerializeField] private InputActionReference _castAction;
        [SerializeField] private float _radius = 5f;
        [SerializeField] private int _damage = 40;
        [SerializeField] private float _cooldown = 6f;
        [SerializeField] private GameObject _vfxPrefab;

        private InputAction _actionInstance;
        private float _lastCastTime = -999f;

        public float Cooldown => _cooldown;
        public float CooldownRemaining => Mathf.Max(0f, _cooldown - (Time.time - _lastCastTime));
        public bool IsReady => CooldownRemaining <= 0f;

        private void OnEnable()
        {
            if (_castAction != null)
            {
                _actionInstance = _castAction.action;
                _actionInstance.performed += OnCastPerformed;
                _actionInstance.Enable();
            }
        }

        private void OnDisable()
        {
            if (_actionInstance != null)
            {
                _actionInstance.performed -= OnCastPerformed;
                _actionInstance.Disable();
                _actionInstance = null;
            }
        }

        private void Update()
        {
            if (_actionInstance != null) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame)
            {
                TryCast();
            }
        }

        private void OnCastPerformed(InputAction.CallbackContext ctx) => TryCast();

        private void TryCast()
        {
            if (!IsReady) return;

            _lastCastTime = Time.time;

            BlueEnemy[] all = FindObjectsByType<BlueEnemy>(FindObjectsSortMode.None);
            Vector3 center = transform.position;
            float rSq = _radius * _radius;

            for (int i = 0; i < all.Length; i++)
            {
                BlueEnemy e = all[i];
                if (e == null || e.IsDead) continue;
                Vector3 delta = e.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude <= rSq)
                {
                    e.TakeDamage(_damage);
                }
            }

            if (_vfxPrefab != null)
            {
                GameObject vfx = Instantiate(_vfxPrefab, center, Quaternion.identity);
                Destroy(vfx, 2f);
            }
        }
    }
}
