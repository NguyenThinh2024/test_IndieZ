#if IAP
using Nexzap.Base.Ads;
using UnityEngine;

namespace Nexzap.Base.IAP
{
    public class IAPRemoteAdsUI : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            AdsController.Instance.OnAdsRemoved += UpdateUI;
        }

        protected virtual void OnDisable()
        {
            AdsController.Instance.OnAdsRemoved -= UpdateUI;
        }

        private void Start()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var isRemoteAds = AdsController.Instance.IsNoAds;
            gameObject.transform.localScale = isRemoteAds? Vector3.zero : Vector3.one;
        }
    }
}
#endif