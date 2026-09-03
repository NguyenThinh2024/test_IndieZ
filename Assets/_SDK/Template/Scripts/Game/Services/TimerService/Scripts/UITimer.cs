using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thinh.Base.Gameplay
{
    public sealed class UITimer : MonoBehaviour
    {
        [Header("Optional - if not assigned, will auto find")]
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image fillImage; // Image Type = Filled

        [Header("Low Time Warning UI")]
        [SerializeField] private float lowTimeWarningSeconds = 15f;
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color warningTextColor = Color.red;
        [SerializeField] private float pulseScale = 1.15f;
        [SerializeField] private float pulseDuration = 0.18f;
        [SerializeField] private bool playClockSound = true;

        private TimerService _timer;
        private Vector3 _originalScale;
        private Tween _pulseTween;

        public void Bind(TimerService timer)
        {
            if (_timer == timer) return;

            Unbind();
            _timer = timer;

            AutoParseIfNeeded();

            if (timeText != null)
                _originalScale = timeText.rectTransform.localScale;
            else
                _originalScale = Vector3.one;

            if (_timer != null)
            {
                _timer.OnChanged += HandleChanged;
                _timer.OnTimeUp += HandleTimeUp;
                _timer.OnLowTimeTick += HandleLowTimeTick;

                HandleChanged(_timer.Remaining, _timer.Duration);
            }
        }

        public void Unbind()
        {
            if (_timer != null)
            {
                _timer.OnChanged -= HandleChanged;
                _timer.OnTimeUp -= HandleTimeUp;
                _timer.OnLowTimeTick -= HandleLowTimeTick;
                _timer = null;
            }

            KillPulse();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void AutoParseIfNeeded()
        {
            if (timeText == null)
                timeText = GetComponentInChildren<TMP_Text>(true);

            if (fillImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i] != null && images[i].name.ToLower().Contains("fill"))
                    {
                        fillImage = images[i];
                        break;
                    }
                }

                if (fillImage == null && images.Length > 0)
                    fillImage = images[0];
            }
        }

        private void HandleChanged(float remaining, float duration)
        {
            if (timeText != null)
            {
                timeText.text = Format(remaining);
                timeText.color = remaining <= lowTimeWarningSeconds ? warningTextColor : normalTextColor;

                if (remaining > lowTimeWarningSeconds)
                {
                    KillPulse();
                    timeText.rectTransform.localScale = _originalScale;
                }
            }

            if (fillImage != null)
            {
                float t = (duration <= 0f) ? 0f : Mathf.Clamp01(remaining / duration);
                fillImage.fillAmount = t;
            }
        }

        private void HandleLowTimeTick(int remainingSecond)
        {
            PulseText();
            PlayClockSound();
        }

        private void HandleTimeUp()
        {
            KillPulse();

            if (timeText != null)
            {
                timeText.color = warningTextColor;
                timeText.rectTransform.localScale = _originalScale;
            }
        }

        private void PulseText()
        {
            if (timeText == null) return;

            KillPulse();

            RectTransform rt = timeText.rectTransform;
            rt.localScale = _originalScale;

            _pulseTween = rt
                .DOScale(_originalScale * pulseScale, pulseDuration)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .OnKill(() =>
                {
                    if (rt != null)
                        rt.localScale = _originalScale;
                });
        }

        private void KillPulse()
        {
            if (_pulseTween != null && _pulseTween.IsActive())
                _pulseTween.Kill();

            _pulseTween = null;
        }

        private void PlayClockSound()
        {
            if (!playClockSound) return;

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySound(SoundName.UI_Clock);
            }
        }

        private static string Format(float seconds)
        {
            int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int m = s / 60;
            int r = s % 60;
            return $"{m:00}:{r:00}";
        }
    }
}