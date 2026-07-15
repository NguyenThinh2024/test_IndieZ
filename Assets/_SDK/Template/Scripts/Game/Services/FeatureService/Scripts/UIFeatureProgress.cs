using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nexzap.Base.Gameplay
{
    public sealed class UIFeatureProgress : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Texts")]
        [SerializeField] private TMP_Text percentText; // "70%"

        [Header("Icon")]
        [SerializeField] private Image featureIconImage;
        [SerializeField] private GameObject questionIconGO;

        [Header("Fill")]
        [SerializeField] private Image fillImage; // Image Type: Filled

        [Header("Anim")]
        [Tooltip("Delay trước khi bắt đầu chạy từ mốc trước -> mốc sau.")]
        [SerializeField] private float stepDelay = 0.25f;

        [Tooltip("Thời gian tween fill từ (current-1)/total -> current/total.")]
        [SerializeField] private float stepDuration = 0.35f;

        [Tooltip("Chạy tween theo unscaled time (khuyên bật cho popup).")]
        [SerializeField] private bool useUnscaledTime = true;

        private FeatureUnlockService _service;
        private Sequence _seq;

        private GameObject GO => root != null ? root : gameObject;

        public void Bind(FeatureUnlockService service)
        {
            if (_service == service) return;

            Unbind();
            _service = service;

            if (_service != null)
                _service.OnNextFeatureChanged += OnNextFeatureChanged;
        }

        public void Unbind()
        {
            if (_service != null)
                _service.OnNextFeatureChanged -= OnNextFeatureChanged;

            _service = null;
        }

        private void OnEnable()
        {
            if (GameController.Instance != null)
                Bind(GameController.Instance.Services.Get<FeatureUnlockService>());

            Refresh(true);
        }

        private void OnDisable()
        {
            KillSeq();
            Unbind();
        }

        private void OnDestroy()
        {
            KillSeq();
            Unbind();
        }

        private void OnNextFeatureChanged(FeatureUnlockConfig _)
        {
            Refresh(true);
        }

        public void Refresh(bool animate)
        {
            if (_service == null)
            {
                KillSeq();
                GO.SetActive(false);
                return;
            }

            if (!_service.TryGetNextFeature(out var next) || next == null)
            {
                KillSeq();
                GO.SetActive(false);
                return;
            }

            GO.SetActive(true);

            var p = _service.GetNextFeatureProgress(); // (passed,total,fill01,...)

            int total = Mathf.Max(1, p.total);
            int current = Mathf.Clamp(p.passed, 0, total);
            int prev = Mathf.Clamp(current - 1, 0, total);

            // progress01 = current/total
            float prevProgress01 = (float)prev / total;
            float targetProgress01 = (float)current / total;

            // inverted fill: fill = 1 - progress
            float prevFillInv = 1f - prevProgress01;
            float targetFillInv = 1f - targetProgress01;

            // set icon sprite (feature icon)
            if (featureIconImage != null)
            {
                featureIconImage.sprite = next.icon;
                featureIconImage.enabled = next.icon != null;
            }

             if (fillImage != null) fillImage.sprite = next.icon;

            bool canAnimate = animate && fillImage != null && stepDuration > 0f && prev != current;

            KillSeq();

            if (!canAnimate)
            {
                ApplyVisual(targetFillInv, targetProgress01);
                return;
            }

            // step: snap về mốc trước
            ApplyVisual(prevFillInv, prevProgress01);

            _seq = DOTween.Sequence().SetUpdate(useUnscaledTime);

            if (stepDelay > 0f)
                _seq.AppendInterval(stepDelay);

            _seq.AppendCallback(() =>
            {
                AudioController.Instance.PlaySound(SoundName.UI_Progress);
            });

            _seq.Append(fillImage.DOFillAmount(targetFillInv, stepDuration).SetEase(Ease.OutCubic));

            _seq.OnUpdate(() =>
            {
                // fillInv đang tween
                float fillInv = Mathf.Clamp01(fillImage.fillAmount);

                // progress thật
                float progress01 = 1f - fillInv;

                // clamp để không vượt target (tránh nhảy do easing/float)
                if (progress01 > targetProgress01) progress01 = targetProgress01;

                ApplyVisual(fillInv, progress01);
            });

            _seq.OnComplete(() =>
            {
                ApplyVisual(targetFillInv, targetProgress01);
                _seq = null;
            });
        }

        private void ApplyVisual(float fillInv01, float progress01)
        {
            progress01 = Mathf.Clamp01(progress01);

            // complete khi ~100%
            bool isComplete = progress01 >= 0.9999f;

            if (isComplete) AudioController.Instance.PlaySound(SoundName.UI_Unlock);

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(fillInv01);

            // ✅ percent chạy mượt theo progress01
            if (percentText != null)
                percentText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";

            if (questionIconGO != null)
                questionIconGO.SetActive(!isComplete);
        }

        private void KillSeq()
        {
            if (_seq != null)
            {
                _seq.Kill();
                _seq = null;
            }
        }
    }
}