using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BlueVfxRing : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _targetRadius = 1f;
        private float _duration = 0.45f;
        private float _initialWidth;
        private float _elapsed;

        public void Init(float targetRadius, float duration)
        {
            _targetRadius = Mathf.Max(0f, targetRadius);
            _duration = Mathf.Max(0.01f, duration);
        }

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _initialWidth = _lr.widthMultiplier;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float radius = Mathf.Lerp(0f, _targetRadius, t);

            int n = _lr.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)n) * Mathf.PI * 2f;
                _lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.05f, Mathf.Sin(a) * radius));
            }
            _lr.widthMultiplier = Mathf.Lerp(_initialWidth, 0f, t);

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
