using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Proto.Sample.BlueArch
{
    /// <summary>
    /// Title 씬의 GuidePanel 용. CanvasGroup.alpha 를 sin 으로 진동시키고,
    /// 마우스 클릭/Space/Enter 입력이 들어오면 다음 씬을 로드한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BlueTitleGuide : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [Tooltip("alpha 진동 주기 (Hz). 1=1초 한 사이클.")]
        [SerializeField] private float _pulseSpeed = 1.0f;
        [SerializeField, Range(0f, 1f)] private float _alphaMin = 0f;
        [SerializeField, Range(0f, 1f)] private float _alphaMax = 1f;
        [Tooltip("클릭 시 로드할 씬 이름 (Build Settings 에 등록되어 있어야 함).")]
        [SerializeField] private string _nextSceneName = "BlueArch";

        private float _t;
        private bool _transitioning;

        private void Awake()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (_transitioning) return;

            if (_group != null)
            {
                _t += Time.unscaledDeltaTime * _pulseSpeed;
                float s = (Mathf.Sin(_t * Mathf.PI * 2f) + 1f) * 0.5f;
                _group.alpha = Mathf.Lerp(_alphaMin, _alphaMax, s);
            }

            if (IsConfirmPressed())
            {
                _transitioning = true;
                SceneManager.LoadScene(_nextSceneName);
            }
        }

        private static bool IsConfirmPressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;
            return false;
        }
    }
}
