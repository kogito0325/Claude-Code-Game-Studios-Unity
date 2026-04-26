using UnityEngine;

namespace Proto.Sample.BlueArch
{
    /// <summary>
    /// 커스텀 마우스 커서. Awake 시 Cursor.SetCursor 로 적용.
    /// CursorMode.Auto 면 Hardware 우선 사용, 실패 시 Software fallback — 빌드 호환.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlueCursor : MonoBehaviour
    {
        [SerializeField] private Texture2D _cursorTexture;
        [Tooltip("커서 핫스팟 (픽셀 좌표, 좌상단 기준). 0,0=이미지 좌상단이 클릭 위치.")]
        [SerializeField] private Vector2 _hotspot = Vector2.zero;
        [SerializeField] private CursorMode _cursorMode = CursorMode.Auto;
        [SerializeField] private bool _applyOnAwake = true;

        private void Awake()
        {
            if (_applyOnAwake) Apply();
        }

        private void OnDisable()
        {
            ResetCursor();
        }

        public void Apply()
        {
            if (_cursorTexture == null) return;
            Cursor.SetCursor(_cursorTexture, _hotspot, _cursorMode);
        }

        public void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
