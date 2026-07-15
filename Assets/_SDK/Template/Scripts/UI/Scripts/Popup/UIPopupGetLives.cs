using TMPro;
using UnityEngine;
using Nexzap.Base.Data;
using Nexzap.Base.Ads;
using Nexzap.Template;

namespace Nexzap.Base.UI.Template
{
    public class UIPopupGetLives : UIBasePopup
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI livesText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI priceText;

        [SerializeField] private UIBaseButton refillBtn;
        [SerializeField] private UIBaseButton adsBtn;

        private void OnEnable()
        {
            refillBtn.onClick.AddListener(OnClickRefill);
            adsBtn.onClick.AddListener(OnClickAds);
        }

        private void OnDisable()
        {
            refillBtn?.onClick?.RemoveListener(OnClickRefill);
            adsBtn?.onClick?.RemoveListener(OnClickAds);
        }

        public override void Show()
        {
            base.Show();
            RefreshUI();
        }

        private void Update()
        {
            if (isActiveAndEnabled) RefreshUI();
        }

        private void RefreshUI()
        {
            var user = UserProfileController.Instance;

            // Lives
            int lives = user.userData.life.liveAmount;
            livesText.text = lives >= LifeData.MAX_LIVES ? $"{LifeData.MAX_LIVES}" : $"{lives}";

            // Timer
            if (user.HasLiveInfinity)
            {
                timerText.text = "∞";
            }
            else if (lives < LifeData.MAX_LIVES)
            {
                int remain = user.NextRefillRemainSeconds;
                timerText.text = GameUtils.ConvertRemainTimeMMSS(remain);
            }
            else
            {
                timerText.text = "FULL";
            }

            // Price refill
            priceText.text = ConfigController.Instance.GameConfig.refillLifePrice.ToString();
        }

        private void OnClickRefill()
        {
            var user = UserProfileController.Instance;
            if (user.UseCoin(ConfigController.Instance.GameConfig.refillLifePrice))
            {
                user.RefillLives();
                RefreshUI();
            }
            else
            {
                Debug.Log("Not enough coins!");
                UIPopupController.Instance.GetActivePopup<UIPopupGetCoins>();
            }
        }

        private void OnClickAds()
        {
            Debug.Log("Play rewarded ads for free life!");
            AdsController.Instance.ShowRewardedAds("get_life", (done) =>
            {
                if (done)
                {
                    UserProfileController.Instance.AddLife(1);
                    RefreshUI();
                }
            });
        }
    }
}
