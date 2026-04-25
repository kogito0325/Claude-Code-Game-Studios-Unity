using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueVfxFade : MonoBehaviour
    {
        private float _duration = 0.15f;
        private float _elapsed;
        private Vector3 _initialScale;

        public void Init(float duration)
        {
            _duration = Mathf.Max(0.01f, duration);
        }

        private void Awake()
        {
            _initialScale = transform.localScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            transform.localScale = Vector3.Lerp(_initialScale, Vector3.zero, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
