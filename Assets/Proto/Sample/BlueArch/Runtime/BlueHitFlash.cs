using System.Collections;
using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueHitFlash : MonoBehaviour
    {
        [SerializeField] private Renderer _target;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _duration = 0.08f;
        [SerializeField] private string _colorProperty = "_BaseColor";

        private MaterialPropertyBlock _mpb;
        private Color _baseColor = Color.white;
        private bool _hasBase;
        private int _propId;
        private Coroutine _co;

        private void Awake()
        {
            if (_target == null) _target = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _propId = Shader.PropertyToID(_colorProperty);
            CacheBase();
        }

        private void CacheBase()
        {
            if (_target == null) return;
            Material shared = _target.sharedMaterial;
            if (shared != null && shared.HasProperty(_propId))
            {
                _baseColor = shared.GetColor(_propId);
                _hasBase = true;
            }
        }

        public void Flash()
        {
            if (_target == null || !_hasBase) return;
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _target.GetPropertyBlock(_mpb);
            _mpb.SetColor(_propId, _flashColor);
            _target.SetPropertyBlock(_mpb);

            yield return new WaitForSeconds(_duration);

            _target.GetPropertyBlock(_mpb);
            _mpb.SetColor(_propId, _baseColor);
            _target.SetPropertyBlock(_mpb);
            _co = null;
        }
    }
}
