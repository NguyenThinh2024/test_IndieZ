using Nexzap.Base.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexzap.Base.Gameplay
{
    public sealed class UINewFeaturePopup : UIBasePopup
    {
        [Header("UI")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Image iconImage;

        [Header("Action")]
        [SerializeField] private UIBaseButton actionButton;
        [SerializeField] private TMP_Text actionButtonText;

        private FeatureUnlockConfig _cfg;

        protected override void Awake()
        {
            base.Awake();

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnClickAction);
            }
        }


        public override void Show()
        {
            AudioController.Instance.PlaySound(SoundName.UI_Unlock);
            base.Show();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (actionButton != null) actionButton.onClick.RemoveAllListeners();
        }

        public void SetData(FeatureUnlockConfig cfg)
        {
            _cfg = cfg;
            if (_cfg == null) return;

            if (headerText != null)
                headerText.text = FeatureUnlockSO.GetHeaderText(_cfg.type);

            if (nameText != null)
                nameText.text = _cfg.featureName ?? "";

            if (descText != null)
                descText.text = _cfg.featureDsc ?? "";

            if (iconImage != null)
            {
                iconImage.sprite = _cfg.icon;
                iconImage.enabled = _cfg.icon != null;
            }

            if (actionButtonText != null)
                actionButtonText.text = FeatureUnlockSO.GetButtonText(_cfg.type);
        }

        private void OnClickAction()
        {
            // Booster => Claim, Obstacle/Item => Continue
            UIPopupController.Instance.HidePopup<UINewFeaturePopup>();
        }
    }
}