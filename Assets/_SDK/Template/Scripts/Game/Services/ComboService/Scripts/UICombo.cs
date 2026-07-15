using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexzap.Base.Gameplay
{
    public sealed class UICombo : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text starTotalText;
        [SerializeField] private Image timerFill;
        [SerializeField] private GameObject root;

        [Header("Star Fly")]
        [SerializeField] private UIStarFlyEmitter starFlyEmitter;

        private ComboService _service;

        private int _pendingStarTotal;
        private bool _hasPendingStar;

        public void Bind(ComboService service)
        {
            if (_service == service) return;
            Unbind();
            _service = service;

            if (_service != null)
            {
                _service.OnChanged += OnChanged;
                _service.OnStarFly += OnStarFly;

                // sync
                _pendingStarTotal = _service.StarTotal;
                _hasPendingStar = true;
                ApplyPendingStarTotal();

                OnChanged(_service.Combo, _service.TimeLeft, 0f, _service.StarTotal);
            }
        }

        public void Unbind()
        {
            if (_service != null)
            {
                _service.OnChanged -= OnChanged;
                _service.OnStarFly -= OnStarFly;
                _service = null;
            }
        }

        private void OnDisable() => Unbind();

        private void OnChanged(int combo, float timeLeft, float window, int starTotal)
        {
            if (root != null) root.SetActive(combo > 0);

            if (comboText != null)
                comboText.text = combo > 0 ? $"COMBO x{combo}" : "";

            // pending, KHÔNG update text ngay
            _pendingStarTotal = starTotal;
            _hasPendingStar = true;

            if (timerFill != null)
            {
                float t = (window <= 0f) ? 0f : Mathf.Clamp01(timeLeft / window);
                timerFill.fillAmount = t;
            }
        }

        private void OnStarFly(int count, Vector3 from)
        {
            if (starFlyEmitter == null)
            {
                ApplyPendingStarTotal();
                return;
            }

            starFlyEmitter.Emit(count, from, ApplyPendingStarTotal);
        }

        private void ApplyPendingStarTotal()
        {
            if (!_hasPendingStar) return;

            if (starTotalText != null)
                starTotalText.text = _pendingStarTotal.ToString();

            _hasPendingStar = false;
        }
    }
}