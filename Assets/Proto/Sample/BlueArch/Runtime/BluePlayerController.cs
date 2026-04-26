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

        [Header("Aim (마우스 ray → aim plane)")]
        [SerializeField] private UnityEngine.Camera _aimCamera;
        [SerializeField] private float _rotateSpeed = 15f;
        [Tooltip("마우스 ray ∩ aim plane 의 Y 높이(플레이어 기준 로컬). 0=발 평면, >0=총구 높이 평면. BlueAutoAttacker 의 muzzle.Y 와 일치하도록 조정 (현 muzzle.Y=1.3).")]
        [SerializeField] private float _aimHeight = 1.3f;

        private MovementAgent _agent;
        private InputAction _moveActionInstance;
        private UnityEngine.Camera _cachedCamera;
        private Vector3 _lastAimDir = Vector3.forward;

        public int MaxHp => _maxHp;
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;

        public System.Action<int, int> HpChanged;
        public System.Action Died;

        private void Awake()
        {
            _agent = GetComponent<MovementAgent>();
            if (_animDriver == null) _animDriver = GetComponent<BlueAnimDriver>();
            _cachedCamera = _aimCamera != null ? _aimCamera : UnityEngine.Camera.main;
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

            // 2) 회전: 마우스 커서가 가리키는 ground 점 방향 (player Y 평면)
            if (TryGetMouseAimDir(out Vector3 aimDir))
            {
                _lastAimDir = aimDir;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(_lastAimDir, Vector3.up), _rotateSpeed * Time.deltaTime);

            // 3) 8-방향 에임-워크: 입력을 캐릭터 로컬 공간으로 변환
            if (_animDriver != null)
            {
                Vector3 localMove = transform.InverseTransformDirection(worldMoveDir);
                _animDriver.DriveDirectional(new Vector2(localMove.x, localMove.z), walkSpeed);
            }
        }

        private bool TryGetMouseAimDir(out Vector3 dir)
        {
            dir = default;
            if (_cachedCamera == null) _cachedCamera = UnityEngine.Camera.main;
            if (_cachedCamera == null) return false;

            Mouse mouse = Mouse.current;
            if (mouse == null) return false;

            Vector2 mp = mouse.position.ReadValue();
            Ray ray = _cachedCamera.ScreenPointToRay(mp);
            Plane plane = new Plane(Vector3.up, transform.position + Vector3.up * _aimHeight);
            if (!plane.Raycast(ray, out float t) || t <= 0f) return false;

            Vector3 hit = ray.GetPoint(t);
            Vector3 to = hit - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-4f) return false;

            dir = to.normalized;
            return true;
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
