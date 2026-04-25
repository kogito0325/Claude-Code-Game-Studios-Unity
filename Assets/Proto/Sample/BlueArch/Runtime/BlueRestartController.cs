using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueRestartController : MonoBehaviour
    {
        [SerializeField] private InputActionReference _restartAction;
        [SerializeField] private Key _fallbackKey = Key.R;

        private InputAction _instance;

        private void OnEnable()
        {
            if (_restartAction != null)
            {
                _instance = _restartAction.action;
                _instance.performed += OnPerformed;
                _instance.Enable();
            }
        }

        private void OnDisable()
        {
            if (_instance != null)
            {
                _instance.performed -= OnPerformed;
                _instance.Disable();
                _instance = null;
            }
        }

        private void Update()
        {
            if (_instance != null) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb[_fallbackKey].wasPressedThisFrame) Restart();
        }

        private void OnPerformed(InputAction.CallbackContext _) => Restart();

        public void Restart()
        {
            Scene s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.name);
        }
    }
}
