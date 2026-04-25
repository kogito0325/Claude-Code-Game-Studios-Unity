using System.Collections.Generic;
using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueEnemySpawnPoint : MonoBehaviour
    {
        private static readonly List<BlueEnemySpawnPoint> _all = new List<BlueEnemySpawnPoint>();

        public static IReadOnlyList<BlueEnemySpawnPoint> All => _all;

        [Tooltip("스폰 시 점 주변 무작위 반경. 0이면 정확히 이 위치.")]
        [SerializeField, Min(0f)] private float _jitterRadius = 0f;

        [Tooltip("Editor Gizmo 색상.")]
        [SerializeField] private Color _gizmoColor = new Color(0.4f, 0.8f, 1f, 0.85f);

        public float JitterRadius => _jitterRadius;

        public Vector3 GetSpawnPosition()
        {
            Vector3 pos = transform.position;
            if (_jitterRadius > 0f)
            {
                Vector2 r = Random.insideUnitCircle * _jitterRadius;
                pos.x += r.x;
                pos.z += r.y;
            }
            return pos;
        }

        private void OnEnable()
        {
            if (!_all.Contains(this)) _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color c = _gizmoColor;
            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, 0.4f);

            c.a = 0.25f;
            Gizmos.color = c;
            if (_jitterRadius > 0f)
            {
                const int seg = 24;
                Vector3 prev = transform.position + new Vector3(_jitterRadius, 0f, 0f);
                for (int i = 1; i <= seg; i++)
                {
                    float a = (i / (float)seg) * Mathf.PI * 2f;
                    Vector3 cur = transform.position + new Vector3(Mathf.Cos(a) * _jitterRadius, 0f, Mathf.Sin(a) * _jitterRadius);
                    Gizmos.DrawLine(prev, cur);
                    prev = cur;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, name);
        }
#endif
    }
}
