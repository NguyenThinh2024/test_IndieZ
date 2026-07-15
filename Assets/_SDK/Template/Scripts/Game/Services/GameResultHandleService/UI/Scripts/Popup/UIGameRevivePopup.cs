using System;
using Nexzap.Base;
using Nexzap.Base.Ads;
using Nexzap.Base.Data;
using Nexzap.Base.Gameplay; // chứa IRevivePopupReceiver + RevivePopupData + FailType
using Nexzap.Base.UI;
using Nexzap.Base.UI.Template;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nexzap.Template
{
    public class UIGameRevivePopup : UIBasePopup
    {
        [Header("Buttons")]
        [SerializeField] private UIBaseButton reviveCoinBtn;
        [SerializeField] private UIBaseButton reviveFreeBtn;
        [SerializeField] private UIBaseButton reviveAdsBtn;
        [SerializeField] private UIBaseButton retryBtn;
        [SerializeField] private UIBaseButton holdToViewBtn;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI coinTxt;

        [Tooltip("Nếu muốn hiển thị mô tả revive theo RevivePopupData.reviveText/failType, kéo Text vào đây.")]
        [SerializeField] private TextMeshProUGUI resonTxt;
        [SerializeField] private TextMeshProUGUI reviveDescTxt;
        [SerializeField] private Image reviveImg;

        // data
        private RevivePopupData _data;
        private bool _hasData;

        // state
        private bool _revived;

        // backward compat (nếu bạn đang dùng ở nơi khác)
        public UnityAction OnRevive;
        public UnityAction OnGiveUp;

        private void OnEnable()
        {
            if (reviveCoinBtn != null) reviveCoinBtn.onClick.AddListener(OnClickReviveCoin);
            if (reviveFreeBtn != null) reviveFreeBtn.onClick.AddListener(OnClickReviveFree);
            if (reviveAdsBtn != null) reviveAdsBtn.onClick.AddListener(OnClickReviveAds);
            if (retryBtn != null) retryBtn.onClick.AddListener(OnClickRetry);

            // Hold events
            if (holdToViewBtn != null)
            {
                holdToViewBtn.onPointerEnter.AddListener(OnHoldDown);
                holdToViewBtn.onPointerExit.AddListener(OnHoldUp);
            } 
        }

        private void OnDisable()
        {
            if (reviveCoinBtn != null) reviveCoinBtn.onClick.RemoveListener(OnClickReviveCoin);
            if (reviveFreeBtn != null) reviveFreeBtn.onClick.RemoveListener(OnClickReviveFree);
            if (reviveAdsBtn != null) reviveAdsBtn.onClick.RemoveListener(OnClickReviveAds);
            if (retryBtn != null) retryBtn.onClick.RemoveListener(OnClickRetry);

            // Hold events
            if (holdToViewBtn != null)
            {
                holdToViewBtn.onPointerEnter.RemoveListener(OnHoldDown);
                holdToViewBtn.onPointerExit.RemoveListener(OnHoldUp);
            }
        }

        // =========================
        // IRevivePopupReceiver
        // =========================
        public void SetData(RevivePopupData data)
        {
            _data = data;
            _hasData = true;

            long coinPrice = data.coinPrice > 0 ? data.coinPrice : ConfigController.Instance.GameConfig.levelRevice;

            if (coinTxt != null)
                coinTxt.text = $"{coinPrice}";

            if (resonTxt != null)
                resonTxt.text = data.reviveText;

            if (reviveDescTxt != null)
                reviveDescTxt.text = data.reviveDscText;    

            if (reviveImg != null)
                reviveImg.sprite = data.reviveSprite;

            bool isEnoughCoin = UserProfileController.Instance.userData.coin >= coinPrice;
            reviveFreeBtn.gameObject.SetActive(!isEnoughCoin);
            reviveCoinBtn.gameObject.SetActive(isEnoughCoin);
        }

        public override void Show()
        {
            base.Show();
            _revived = false;
        }

        protected override void HideCompleted()
        {
            base.HideCompleted();

            if (_revived)
            {
                OnRevive?.Invoke();
            }
            else
            {
                OnGiveUp?.Invoke();
            }

            OnRevive = null;
            OnGiveUp = null;
        }

        // =========================
        // Click handlers
        // =========================
        private void OnClickReviveCoin()
        {
            long coinPrice = _data.coinPrice > 0 ? _data.coinPrice : ConfigController.Instance.GameConfig.levelRevice;

            if (UserProfileController.Instance.UseCoin(coinPrice))
            {
                AudioController.Instance.PlaySound(SoundName.UI_Revive);
                _revived = true;
                UIPopupController.Instance.HidePopup<UIGameRevivePopup>();
            }
            else
            {
                AudioController.Instance.PlaySound(SoundName.UI_Invalid);
                UIPopupController.Instance.GetActivePopup<UIPopupGetCoins>();
            }
        }

        private void OnClickReviveFree()
        {
            AudioController.Instance.PlaySound(SoundName.UI_Revive);
            _revived = true;
            UIPopupController.Instance.HidePopup<UIGameRevivePopup>();
        }

        private void OnClickReviveAds()
        {
            string placement = string.IsNullOrEmpty(_data.rewardedPlacement) ? "revive" : _data.rewardedPlacement;

            AdsController.Instance.ShowRewardedAds(placement, (done) =>
            {
                if (!done) return;

                AudioController.Instance.PlaySound(SoundName.UI_Revive);
                _revived = true;
                UIPopupController.Instance.HidePopup<UIGameRevivePopup>();
            });
        }

        private void OnClickRetry()
        {
            // Clear callbacks to prevent triggering lose flow during scene reload
            OnRevive = null;
            OnGiveUp = null;
            if (UIPopupController.Instance != null)
                UIPopupController.Instance.HideAllPopups();
            UISceneController.Instance.ReloadCurrentScene();
        }

        // =========================
        // HOLD LOGIC
        // =========================
        private void OnHoldDown()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
        }

        private void OnHoldUp()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 1f;
        }
    }
}