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

        public GameState State { get; private set; } = GameState.Playing;
        public float Elapsed { get; private set; }
        public float TimeRemaining => Mathf.Max(0f, _winDuration - Elapsed);
        public int Score { get; private set; }

        public System.Action<GameState> StateChanged;
        public System.Action<int> ScoreChanged;

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
