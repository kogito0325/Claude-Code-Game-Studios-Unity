using UnityEngine;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueGameManager : MonoBehaviour
    {
        public enum GameState { Playing, Win, Lose }

        [SerializeField] private BluePlayerController _player;
        [SerializeField] private BlueEnemySpawner _spawner;
        [SerializeField] private float _winDuration = 180f;

        [Header("Frame Rate Stabilization")]
        [Tooltip("타겟 fps. 화면 크기 변동 시 deltaTime 변동을 줄여 충돌·이동 판정 일관성 유지. -1=무제한.")]
        [SerializeField] private int _targetFrameRate = 60;
        [Tooltip("vSyncCount. 0=vsync 끔(targetFrameRate 강제), 1=vsync 켬.")]
        [SerializeField] private int _vSyncCount = 0;
        [Tooltip("한 프레임 시뮬레이션 최대 deltaTime(초). fps 가 매우 떨어져도 이보다 큰 deltaTime 으로 simulate 안 함.")]
        [SerializeField] private float _maximumDeltaTime = 0.05f;

        public GameState State { get; private set; } = GameState.Playing;
        public float Elapsed { get; private set; }
        public float TimeRemaining => Mathf.Max(0f, _winDuration - Elapsed);
        public int Score { get; private set; }

        public System.Action<GameState> StateChanged;
        public System.Action<int> ScoreChanged;

        private void Awake()
        {
            QualitySettings.vSyncCount = _vSyncCount;
            Application.targetFrameRate = _targetFrameRate;
            if (_maximumDeltaTime > 0f) Time.maximumDeltaTime = _maximumDeltaTime;
        }

        private void Start()
        {
            if (_player != null) _player.Died += OnPlayerDied;
        }

        private void OnDestroy()
        {
            if (_player != null) _player.Died -= OnPlayerDied;
        }

        private void OnEnable()
        {
            BlueEnemy.OnAnyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            BlueEnemy.OnAnyKilled -= OnEnemyKilled;
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            Elapsed += Time.deltaTime;
            if (Elapsed >= _winDuration)
            {
                Transition(GameState.Win);
            }
        }

        private void OnPlayerDied()
        {
            if (State == GameState.Playing) Transition(GameState.Lose);
        }

        private void OnEnemyKilled(BlueEnemy _)
        {
            if (State != GameState.Playing) return;
            Score += 10;
            ScoreChanged?.Invoke(Score);
        }

        private void Transition(GameState next)
        {
            if (State == next) return;
            State = next;
            if (_spawner != null) _spawner.SetActive(false);
            StateChanged?.Invoke(next);
        }
    }
}
