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
        [SerializeField] private BlueAutoAttacker _autoAttacker;

        [Header("Rotation")]
        [SerializeField] private float _rotateSpeed = 15f;

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
            if (_autoAttacker == null) _autoAttacker = GetComponent<BlueAutoAttacker>();
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
            Vector3 worldMoveDir = new Vector3(input.x, 0f, input.y);
            float inputMag = worldMoveDir.magnitude;

            // 1) 이동
            float walkSpeed = 0f;
            if (inputMag >= 1e-2f)
            {
                Vector3 worldDelta = worldMoveDir * (_moveSpeed * Time.deltaTime);
                _agent.MoveBy(worldDelta);
                walkSpeed = _moveSpeed * inputMag;
            }

            // 2) 회전: 적이 있으면 적 방향, 없으면 이동 방향
            Vector3 facingTarget = Vector3.zero;
            if (_autoAttacker != null && _autoAttacker.HasTarget)
            {
                Vector3 toTarget = _autoAttacker.CurrentTarget.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 1e-4f) facingTarget = toTarget.normalized;
            }
            else if (inputMag >= 1e-2f)
            {
                facingTarget = worldMoveDir.normalized;
            }

            if (facingTarget.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(facingTarget, Vector3.up), _rotateSpeed * Time.deltaTime);
            }

            // 3) 8-방향 에임-워크: 입력을 캐릭터 로컬 공간으로 변환
            if (_animDriver != null)
            {
                Vector3 localMove = transform.InverseTransformDirection(worldMoveDir);
                _animDriver.DriveDirectional(new Vector2(localMove.x, localMove.z), walkSpeed);
            }
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
