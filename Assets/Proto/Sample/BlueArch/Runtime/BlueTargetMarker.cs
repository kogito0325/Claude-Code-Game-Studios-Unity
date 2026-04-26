using UnityEngine;

namespace Proto.Sample.BlueArch
{
    /// <summary>
    /// 적 머리 위 표시용 sprite 마커. 매 LateUpdate 마다 sin 펄스로 scale 진동 +
    /// (옵션) 카메라 정면 향함.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlueTargetMarker : MonoBehaviour
    {
        [SerializeField] private float _pulseSpeed = 2f;
        [Tooltip("scale 진폭. 0=비활성, 0.2=base 의 ±20%.")]
        [SerializeField, Range(0f, 1f)] private float _pulseAmount = 0.2f;
        [SerializeField] private Vector3 _baseScale = new Vector3(0.8f, 0.8f, 1f);
        [Tooltip("true=카메라 정면을 항상 향함 (탑뷰에선 sprite 가 카메라 평면에 평행).")]
        [SerializeField] private bool _faceCamera = true;

        private float _t;
        private UnityEngine.Camera _cam;

        public void Configure(float pulseSpeed, float pulseAmount, Vector3 baseScale, bool faceCamera = true)
        {
            _pulseSpeed = pulseSpeed;
            _pulseAmount = pulseAmount;
            _baseScale = baseScale;
            _faceCamera = faceCamera;
        }

        private void Start()
        {
            _cam = UnityEngine.Camera.main;
            transform.localScale = _baseScale;
        }

        private void LateUpdate()
        {
            _t += Time.deltaTime * _pulseSpeed;
            float p = 1f + Mathf.Sin(_t * Mathf.PI * 2f) * _pulseAmount;
            transform.localScale = new Vector3(_baseScale.x * p, _baseScale.y * p, _baseScale.z);

            if (_faceCamera)
            {
                if (_cam == null) _cam = UnityEngine.Camera.main;
                if (_cam != null) transform.rotation = _cam.transform.rotation;
            }
        }
    }
}
