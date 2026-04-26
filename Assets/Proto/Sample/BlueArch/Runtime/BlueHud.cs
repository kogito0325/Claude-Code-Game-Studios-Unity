using System.Collections;
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

        [Header("Result Screen (페이드인)")]
        [Tooltip("결과 패널 루트의 CanvasGroup. alpha 0→1 으로 페이드 인.")]
        [SerializeField] private CanvasGroup _resultGroup;
        [SerializeField] private Image _victoryImage;
        [SerializeField] private Image _failedImage;
        [Tooltip("페이드인 지속 시간(초). Time.unscaledDeltaTime 기준.")]
        [SerializeField] private float _resultFadeDuration = 0.8f;
        [Tooltip("결과 화면 좌측: 생존 시간/점수 표시.")]
        [SerializeField] private TMP_Text _resultLeftStats;
        [Tooltip("결과 화면 우측: R 키 재시작 안내.")]
        [SerializeField] private TMP_Text _resultRightHint;
        [Tooltip("우측 안내 문구.")]
        [SerializeField] private string _restartHintText = "R 키를 눌러 다시 시작";

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

            if (_resultGroup != null)
            {
                _resultGroup.alpha = 0f;
                _resultGroup.gameObject.SetActive(false);
            }
            if (_victoryImage != null) _victoryImage.gameObject.SetActive(false);
            if (_failedImage != null) _failedImage.gameObject.SetActive(false);
            if (_resultLeftStats != null) _resultLeftStats.gameObject.SetActive(false);
            if (_resultRightHint != null) _resultRightHint.gameObject.SetActive(false);
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
            Image showImage = null;
            switch (s)
            {
                case BlueGameManager.GameState.Win:  showImage = _victoryImage; break;
                case BlueGameManager.GameState.Lose: showImage = _failedImage; break;
                default: return;
            }

            if (_resultGroup != null && showImage != null)
            {
                if (_victoryImage != null) _victoryImage.gameObject.SetActive(showImage == _victoryImage);
                if (_failedImage != null)  _failedImage.gameObject.SetActive(showImage == _failedImage);

                if (_resultLeftStats != null)
                {
                    _resultLeftStats.text = BuildStatsText();
                    _resultLeftStats.gameObject.SetActive(true);
                }
                if (_resultRightHint != null)
                {
                    _resultRightHint.text = _restartHintText;
                    _resultRightHint.gameObject.SetActive(true);
                }

                _resultGroup.gameObject.SetActive(true);
                _resultGroup.alpha = 0f;
                StopAllCoroutines();
                StartCoroutine(FadeInResult());
            }

            if (_resultText != null) _resultText.gameObject.SetActive(false);
        }

        private string BuildStatsText()
        {
            int score = _gameManager != null ? _gameManager.Score : 0;
            float survived = _gameManager != null ? _gameManager.Elapsed : 0f;
            int mm = Mathf.FloorToInt(survived / 60f);
            int ss = Mathf.FloorToInt(survived - mm * 60f);
            return $"생존 시간\n{mm:0}:{ss:00}\n\n처치 점수\n{score}";
        }

        private IEnumerator FadeInResult()
        {
            if (_resultGroup == null) yield break;
            float dur = Mathf.Max(0.0001f, _resultFadeDuration);
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _resultGroup.alpha = Mathf.Clamp01(t / dur);
                yield return null;
            }
            _resultGroup.alpha = 1f;
        }
    }
}
