using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Proto.Sample.BlueArch
{
    [DisallowMultipleComponent]
    public sealed class BlueHud : MonoBehaviour
    {
        [SerializeField] private BluePlayerController _player;
        [SerializeField] private BlueGameManager _gameManager;
        [SerializeField] private BlueSkill _skill;

        [Header("Widgets")]
        [SerializeField] private Slider _hpBar;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private Image _skillCooldownFill;
        [SerializeField] private TMP_Text _resultText;

        private void Start()
        {
            if (_player != null)
            {
                _player.HpChanged += OnHpChanged;
                UpdateHp(_player.Hp, _player.MaxHp);
            }
            if (_gameManager != null)
            {
                _gameManager.StateChanged += OnStateChanged;
                _gameManager.ScoreChanged += OnScoreChanged;
            }
            if (_resultText != null) _resultText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_player != null) _player.HpChanged -= OnHpChanged;
            if (_gameManager != null)
            {
                _gameManager.StateChanged -= OnStateChanged;
                _gameManager.ScoreChanged -= OnScoreChanged;
            }
        }

        private void Update()
        {
            if (_gameManager != null && _timerText != null)
            {
                float t = _gameManager.TimeRemaining;
                int mm = Mathf.FloorToInt(t / 60f);
                int ss = Mathf.FloorToInt(t - mm * 60f);
                _timerText.text = $"{mm:0}:{ss:00}";
            }

            if (_skill != null && _skillCooldownFill != null)
            {
                float cd = _skill.Cooldown <= 0f ? 0f : _skill.CooldownRemaining / _skill.Cooldown;
                _skillCooldownFill.fillAmount = cd;
            }
        }

        private void OnHpChanged(int hp, int max) => UpdateHp(hp, max);

        private void UpdateHp(int hp, int max)
        {
            if (_hpBar != null)
            {
                _hpBar.minValue = 0;
                _hpBar.maxValue = max;
                _hpBar.value = hp;
            }
            if (_hpText != null) _hpText.text = $"{hp} / {max}";
        }

        private void OnScoreChanged(int s)
        {
            if (_scoreText != null) _scoreText.text = $"Score: {s}";
        }

        private void OnStateChanged(BlueGameManager.GameState s)
        {
            if (_resultText == null) return;
            switch (s)
            {
                case BlueGameManager.GameState.Win:
                    _resultText.gameObject.SetActive(true);
                    _resultText.text = "VICTORY\n\nPress R to Restart";
                    break;
                case BlueGameManager.GameState.Lose:
                    _resultText.gameObject.SetActive(true);
                    _resultText.text = "DEFEAT\n\nPress R to Restart";
                    break;
            }
        }
    }
}
