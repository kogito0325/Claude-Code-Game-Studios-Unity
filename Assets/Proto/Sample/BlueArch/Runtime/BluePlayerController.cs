using Proto.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MovementAgent))]
    public sealed class BluePlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private InputActionReference _moveAction;

        [Header("HP")]
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private BlueHitFlash _hitFlash;
        [SerializeField] private AudioClip _hurtSfx;
        [SerializeField] private BlueAnimDriver _animDriver;

        private MovementAgent _agent;
        private InputAction _moveActionInstance;

        public int MaxHp => _maxHp;
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;

        public System.Action<int, int> HpChanged;
        public System.Action Died;

        private void Awake()
        {
            _agent = GetComponent<MovementAgent>();
            if (_animDriver == null) _animDriver = GetComponent<BlueAnimDriver>();
            Hp = _maxHp;
        }

        private void OnEnable()
        {
            if (_moveAction != null)
            {
                _moveActionInstance = _moveAction.action;
                _moveActionInstance.Enable();
            }
        }

        private void OnDisable()
        {
            if (_moveActionInstance != null)
            {
                _moveActionInstance.Disable();
                _moveActionInstance = null;
            }
        }

        private void Update()
        {
            if (IsDead) return;

            Vector2 input = ReadMoveInput();
            float walkSpeed = 0f;

            if (input.sqrMagnitude >= 1e-4f)
            {
                Vector3 worldDelta = new Vector3(input.x, 0f, input.y) * (_moveSpeed * Time.deltaTime);
                _agent.MoveBy(worldDelta);
                walkSpeed = _moveSpeed * input.magnitude;

                Vector3 flatDir = new Vector3(input.x, 0f, input.y);
                if (flatDir.sqrMagnitude > 1e-4f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(flatDir, Vector3.up), 15f * Time.deltaTime);
                }
            }

            if (_animDriver != null) _animDriver.DriveLocomotion(walkSpeed);
        }

        private Vector2 ReadMoveInput()
        {
            if (_moveActionInstance != null)
            {
                return _moveActionInstance.ReadValue<Vector2>();
            }

            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            Vector2 v = Vector2.zero;
            if (kb.wKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed) v.y -= 1f;
            if (kb.dKey.isPressed) v.x += 1f;
            if (kb.aKey.isPressed) v.x -= 1f;
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            Hp = Mathf.Max(0, Hp - amount);
            HpChanged?.Invoke(Hp, _maxHp);
            if (_hitFlash != null) _hitFlash.Flash();
            BlueSfx.Play(_hurtSfx, transform.position);
            if (Hp <= 0)
            {
                if (_animDriver != null) _animDriver.TriggerDie();
                Died?.Invoke();
            }
        }
    }
}
