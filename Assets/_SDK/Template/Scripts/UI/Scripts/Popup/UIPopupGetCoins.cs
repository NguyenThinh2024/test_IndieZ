using TMPro;
using UnityEngine;
using Thinh.Base.Data;
using Thinh.Base.Ads;
using Thinh.Template;

namespace Thinh.Base.UI.Template
{
    public class UIPopupGetCoins : UIBasePopup
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private UIBaseButton adsBtn;


        private void OnEnable()
        {
            adsBtn.onClick.AddListener(OnClickAds);
        }

        private void OnDisable()
        {
            adsBtn?.onClick?.RemoveListener(OnClickAds);
        }

        public override void Show()
        {
            base.Show();
            RefreshUI();
        }

        private void RefreshUI()
        {
            rewardText.text = $"{ConfigController.Instance.GameConfig.freeGetCoin}";
        }

        private void OnClickAds()
        {
            Debug.Log("Play rewarded ads for free coins!");
            var user = UserProfileController.Instance;

            AdsController.Instance.ShowRewardedAds(
                "free_coin",
                (done) =>
                {
                    if (done)
                    {
                        UserProfileController.Instance.AddCoin(ConfigController.Instance.GameConfig.freeGetCoin);
                        RefreshUI();
                    }
                });
        }
    }
}
