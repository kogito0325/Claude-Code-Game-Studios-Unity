using Proto.Camera;
using Proto.Testing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Proto.Sample.BlueArch
{
    /// <summary>
    /// CameraRig 의 IFramingTarget 으로 등록되어 카메라 앵커 위치를 제공한다.
    /// 앵커 = SmoothDamp(이전, Lerp(player.position, mouseWorldOnPlayerPlane, _aimFactor), _smoothTime)
    /// 단, 플레이어로부터 _maxOffset 이내로 클램프.
    /// 같은 GameObject 의 ProtoUnitTarget(있을 경우) 은 활성 동안 unregister 하여
    /// 본 컴포넌트가 단독 앵커가 되도록 한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class BlueCameraAimOffset : MonoBehaviour, IFramingTarget
    {
        [Header("Refs (auto-resolved if empty)")]
        [SerializeField] private CameraRig _rig;
        [SerializeField] private UnityEngine.Camera _aimCamera;
        [SerializeField] private Transform _player;

        [Header("Aim Offset")]
        [Tooltip("플레이어→마우스 방향 보간 비율. 0=고정, 1=완전 마우스 위치.")]
        [SerializeField, Range(0f, 1f)] private float _aimFactor = 0.4f;

        [Tooltip("플레이어 기준 앵커가 벗어날 수 있는 최대 거리(unit). 0 이하면 클램프 비활성.")]
        [SerializeField] private float _maxOffset = 7f;

        [Tooltip("앵커 보간 시간(초). 작을수록 즉각 반응.")]
        [SerializeField] private float _smoothTime = 0.12f;

        [Header("Framing Target")]
        [SerializeField, Range(0f, 1f)] private float _weight = 1f;

        private ProtoUnitTarget _suppressedSibling;
        private Vector3 _current;
        private Vector3 _velocity;

        public Vector3 Position => _current;
        public float Weight => _weight;

        private void Awake()
        {
            if (_player == null) _player = transform;
            if (_aimCamera == null) _aimCamera = UnityEngine.Camera.main;
            if (_rig == null) _rig = FindFirstObjectByType<CameraRig>();
            _current = _player != null ? _player.position : transform.position;
        }

        private void Start()
        {
            if (_rig == null) return;

            _suppressedSibling = GetComponent<ProtoUnitTarget>();
            if (_suppressedSibling != null) _rig.UnregisterTarget(_suppressedSibling);
            _rig.RegisterTarget(this);
        }

        private void OnDestroy()
        {
            if (_rig == null) return;
            _rig.UnregisterTarget(this);
            if (_suppressedSibling != null)
            {
                _rig.RegisterTarget(_suppressedSibling);
                _suppressedSibling = null;
            }
        }

        private void Update()
        {
            if (_player == null) return;

            Vector3 playerPos = _player.position;
            Vector3 desired = playerPos;

            if (TryGetMouseWorldOnPlayerPlane(playerPos, out Vector3 mouseWorld))
            {
                Vector3 offset = (mouseWorld - playerPos) * _aimFactor;
                offset.y = 0f;
                if (_maxOffset > 0f)
                {
                    float mag = offset.magnitude;
                    if (mag > _maxOffset) offset *= _maxOffset / mag;
                }
                desired = playerPos + offset;
            }

            _current = Vector3.SmoothDamp(_current, desired, ref _velocity, Mathf.Max(0f, _smoothTime));
        }

        private bool TryGetMouseWorldOnPlayerPlane(Vector3 playerPos, out Vector3 hit)
        {
            hit = default;
            UnityEngine.Camera cam = _aimCamera != null ? _aimCamera : UnityEngine.Camera.main;
            if (cam == null) return false;

            Mouse mouse = Mouse.current;
            if (mouse == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.up, playerPos);
            if (!plane.Raycast(ray, out float t) || t <= 0f) return false;

            hit = ray.GetPoint(t);
            return true;
        }
    }
}
