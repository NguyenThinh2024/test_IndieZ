#if IAP
using UnityEngine;
using TMPro;
using Thinh.Base.UI;
using UnityEngine.Purchasing;

namespace Thinh.Base.IAP
{
    public class IAPButtonUI : MonoBehaviour
    {
        [Header("IAP Settings")]
        public IAPProductType productType;

        [Header("UI References")]
        public UIBaseButton buyButton;
        public TMP_Text priceText;

        private void Start()
        {
            buyButton.onClick.AddListener(OnBuyClick);

            if (IAPController.HasInstance)
                RefreshUI();
            else
                IAPController.OnInstanceReady += _ => RefreshUI();
        }

        private void RefreshUI()
        {
            var controller = IAPController.Instance;
            string storeId = IAPIdMap.GetID(productType);
            var product = controller.GetUnityProduct(storeId);
            buyButton.transform.localScale= Vector3.one;
            if (product == null)
            {
                priceText.text = "...";
                buyButton.button.interactable = false;
                return;
            }

            // ===== NON-CONSUMABLE & ĐÃ MUA =====
            if (product.definition.type == ProductType.NonConsumable &&
                product.hasReceipt)
            {
                buyButton.transform.localScale = Vector3.zero;
                priceText.text = "Purchased";
                buyButton.button.interactable = false;
                return;
            }

            // ===== CHƯA MUA / CONSUMABLE =====
            priceText.text = product.metadata.localizedPriceString;
            buyButton.button.interactable = true;
        }

        private void OnBuyClick()
        {
            var controller = IAPController.Instance;

            // Chặn click nếu là Non-Consumable đã mua
            string storeId = IAPIdMap.GetID(productType);
            var product = controller.GetUnityProduct(storeId);

            if (product != null &&
                product.definition.type == ProductType.NonConsumable &&
                product.hasReceipt)
            {
                Debug.Log($"[IAP] Already purchased: {productType}");
                return;
            }

            controller.BuyProduct(productType, success =>
            {
                if (success)
                {
                    Debug.Log($"[IAP] Purchase success: {productType}");
                    RefreshUI(); // cập nhật lại UI ngay sau mua
                }
                else
                {
                    Debug.LogError($"[IAP] Purchase failed: {productType}");
                }
            });
        }
    }
}
#endif