#if IAP
using UnityEngine;

namespace Thinh.Base.IAP
{
    /// <summary>
    /// Base class cho mọi reward handler – tự đăng ký với IAPController.
    /// </summary>
    public abstract class BaseIAPRewardHandler : MonoBehaviour, IIAPRewardHandler
    {
        protected virtual void Awake()
        {
            if (IAPController.HasInstance)
            {
                IAPController.Instance.SetRewardHandler(this);
            }
            else
            {
                IAPController.OnInstanceReady += OnIAPControllerReady;
            }
        }

        private void OnDestroy()
        {
            IAPController.OnInstanceReady -= OnIAPControllerReady;
        }

        private void OnIAPControllerReady(IAPController controller)
        {
            controller.SetRewardHandler(this);
        }

        /// <summary>
        /// Đây là method được gọi từ IAPController sau khi purchase thành công.
        /// </summary>
        public void OnRewardGranted(IAPProductType productType)
        {
            Debug.Log($"[IAPRewardHandler] Reward granted for: {productType}");
            HandleReward(productType);
        }

        /// <summary>
        /// Mỗi game sẽ override logic thưởng theo từng loại IAP.
        /// </summary>
        protected abstract void HandleReward(IAPProductType type);
    }
}
#endif