#if IAP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

#if APPSFLYER
using AppsFlyerSDK;
#endif

using UnityEngine.Purchasing.Security;

namespace Nexzap.Base.IAP
{
    public class IAPController : MonoSingleton<IAPController>, IStoreListener
    {
        public static event Action<IAPController> OnInstanceReady;
        public static bool HasInstance => Instance != null;

        private IStoreController storeController;
        private IExtensionProvider extensionProvider;

        private IIAPRewardHandler rewardHandler;

        [SerializeField] private IAPSettings settings;

        private readonly Dictionary<IAPProductType, Action<bool>> purchaseCallbacks = new();

        // ===============================
        // CONFIG
        // ===============================
        private const double AF_REVENUE_RATE = 0.65; // 🔥 Net revenue

        // ===============================
        // INIT
        // ===============================
        public override void Init()
        {
            base.Init();
            OnInstanceReady?.Invoke(this);
        }

        public void InitializePurchasing()
        {
            if (IsInitialized()) return;

            var builder = ConfigurationBuilder.Instance(
                StandardPurchasingModule.Instance(AppStore.GooglePlay)
            );

            foreach (var kvp in settings.Products)
            {
                IAPProductType iapType = kvp.Key;
                ProductType productType = kvp.Value;

                string id = IAPIdMap.GetID(iapType);

                builder.AddProduct(id, productType);

                Debug.Log($"[IAP] Register product: {iapType} | {productType}");
            }

            UnityPurchasing.Initialize(this, builder);
        }


        private bool IsInitialized() =>
            storeController != null && extensionProvider != null;

        // ===============================
        // PUBLIC API
        // ===============================
        public void SetRewardHandler(IIAPRewardHandler handler)
        {
            rewardHandler = handler;
            Debug.Log($"[IAP] RewardHandler registered: {handler.GetType().Name}");
        }

        public void BuyProduct(IAPProductType type, Action<bool> onComplete = null)
        {
            if (!IsInitialized())
            {
                Debug.LogWarning("[IAP] Not initialized.");
                onComplete?.Invoke(false);
                return;
            }

            string id = IAPIdMap.GetID(type);
            var product = storeController.products.WithID(id);

            if (product != null && product.availableToPurchase)
            {
                purchaseCallbacks[type] = onComplete;
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogWarning($"[IAP] Product unavailable: {type}");
                onComplete?.Invoke(false);
            }
        }

        // ===============================
        // IStoreListener
        // ===============================
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            extensionProvider = extensions;
            Debug.Log("[IAP] Initialized successfully.");

            AutoRestoreNonConsumables();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[IAP] Initialization failed: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[IAP] Initialization failed: {error} - {message}");
        }

        private bool ValidatePurchase(Product product)
        {
#if UNITY_EDITOR
            return true;

#else
    if (product == null || string.IsNullOrEmpty(product.receipt))
        return false;

    try
    {
        CrossPlatformValidator validator;

#if UNITY_ANDROID
        validator = new CrossPlatformValidator(
            GooglePlayTangle.Data(),
            null,
            Application.identifier
        );

#elif UNITY_IOS
        validator = new CrossPlatformValidator(
            null,
            AppleTangle.Data(),
            Application.identifier
        );

#else
        return false;
#endif

        var result = validator.Validate(product.receipt);

        foreach (IPurchaseReceipt receipt in result)
        {
            // 👉 Mua trong 5 phút gần đây = mua mới
            if (receipt.purchaseDate >= DateTime.UtcNow.AddMinutes(-5))
                return true;
        }

        // 👉 Receipt cũ → restore
        return false;
    }
    catch (Exception e)
    {
        Debug.LogError($"[IAP] Validate error: {e}");
        return false;
    }
#endif
        }

        private void AutoRestoreNonConsumables()
        {
            if (!IsInitialized()) return;

            Debug.Log("[IAP] Auto restore non-consumables");

            foreach (var kvp in settings.Products)
            {
                IAPProductType type = kvp.Key;
                ProductType productType = kvp.Value;

                // ❗ Chỉ restore NON-CONSUMABLE
                if (productType != ProductType.NonConsumable)
                    continue;

                string storeId = IAPIdMap.GetID(type);
                var product = storeController.products.WithID(storeId);

                if (product == null)
                    continue;

                if (product.hasReceipt)
                {
                    Debug.Log($"[IAP] Restored: {type}");

                    // ⚠️ KHÔNG gọi TrackAppsFlyerRevenue
                    // ⚠️ KHÔNG ValidatePurchase

                    RewardPlayer(type);
                }
            }
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Product product = args.purchasedProduct;
            string storeId = product.definition.id;
            IAPProductType type = GetEnumFromStoreID(storeId);

            bool isNewPurchase = ValidatePurchase(product);

            if (isNewPurchase)
            {
                // ===============================
                // TRACK APPSFLYER REVENUE ONLY
                // af_revenue = price * 0.65
                // ===============================
                TrackAppsFlyerRevenue(product);
            }
            else
            {
                Debug.Log($"[IAP] Restore detected – skip AF: {type}");
            }

            if (purchaseCallbacks.TryGetValue(type, out var callback))
            {
                callback?.Invoke(true);
                purchaseCallbacks.Remove(type);
            }

            RewardPlayer(type);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            string storeId = product.definition.id;
            IAPProductType type = GetEnumFromStoreID(storeId);

            if (purchaseCallbacks.TryGetValue(type, out var callback))
            {
                callback?.Invoke(false);
                purchaseCallbacks.Remove(type);
            }

            Debug.LogWarning($"[IAP] Purchase failed: {type} | {failureReason}");
        }

        // ===============================
        // APPSFLYER TRACKING
        // ===============================
        private void TrackAppsFlyerRevenue(Product product)
        {
            if (product == null || product.metadata == null) return;

            double rawPrice = (double)product.metadata.localizedPrice;
            double afRevenue = rawPrice * AF_REVENUE_RATE;
            string currency = product.metadata.isoCurrencyCode;

            var eventValues = new Dictionary<string, string>
            {
                { AFInAppEvents.REVENUE, afRevenue.ToString("F2") },
                { AFInAppEvents.CURRENCY, currency },
                { AFInAppEvents.CONTENT_ID, product.definition.id },
                { AFInAppEvents.CONTENT_TYPE, "iap" }
            };

            Debug.Log($"[IAP][AF] af_purchase | raw={rawPrice} | revenue={afRevenue} {currency}");

            AppsFlyer.sendEvent(AFInAppEvents.PURCHASE, eventValues);
        }

        // ===============================
        // REWARD
        // ===============================
        private void RewardPlayer(IAPProductType type)
        {
            if (rewardHandler != null)
                rewardHandler.OnRewardGranted(type);
            else
                Debug.LogWarning("[IAP] No reward handler assigned!");
        }

        private IAPProductType GetEnumFromStoreID(string storeId)
        {
            foreach (var kvp in settings.Products)
            {
                IAPProductType type = kvp.Key;

                if (IAPIdMap.GetID(type) == storeId)
                    return type;
            }

            Debug.LogError($"[IAP] Unknown store ID: {storeId}");
            return IAPProductType.Fail;
        }


        public Product GetUnityProduct(string storeId)
        {
            if (storeController == null) return null;
            return storeController.products.WithID(storeId);
        }
    }

    public interface IIAPRewardHandler
    {
        void OnRewardGranted(IAPProductType type);
    }
}
#endif