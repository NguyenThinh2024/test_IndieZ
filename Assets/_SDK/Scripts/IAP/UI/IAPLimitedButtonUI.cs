using UnityEngine;
using TMPro;
using System;
using Nexzap.Base.UI;
using Nexzap.Base.Data;
namespace Nexzap.Base.IAP

{
    public class IAPLimitedButtonUI : MonoBehaviour
    {
        [Header("UI References")]
        public UIBaseButton buyButton;        // Hiển thị giá

        private void Awake()
        {
            if (buyButton == null)
            {
                Debug.LogWarning($"{nameof(IAPLimitedButtonUI)}: buyButton chưa được gán!");
                return;
            }

            buyButton.onClick.AddListener(ShowSpecialDialog);
        }

        private void Start()
        {
            var curLevel = UserProfileController.Instance.GetParam<int>("currentLevel");
            if (curLevel >= 5 )
            {
                bool haveTutorial = UserProfileController.Instance.GetParam<int>("call_outfit_tutorial") > 0;
                if(haveTutorial) return;
                ShowSpecialDialog();
            }
        }

        private void OnDestroy()
        {
            buyButton?.onClick.RemoveListener(ShowSpecialDialog);
        }

        private void ShowSpecialDialog()
        {
            var dialogManager = UIPopupController.Instance;
            if (dialogManager == null)
            {
                Debug.LogWarning("UIDialogManager chưa sẵn sàng để mở dialog đặc biệt.");
                return;
            }

            dialogManager.GetActivePopup<UIIAPSpecialPopup>();
        }
    }
}
