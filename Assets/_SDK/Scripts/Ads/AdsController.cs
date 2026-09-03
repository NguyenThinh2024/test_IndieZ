using System;
using Thinh.Base;
using UnityEngine;

namespace Thinh.Base.Ads
{
    public class AdsController : MonoSingleton<AdsController>
    {
        [SerializeField] private bool autoCompleteRewardedAds = true;

        private bool isNoAds;

        public event Action OnAdsRemoved;

        public bool IsNoAds
        {
            get => isNoAds;
            set
            {
                if (isNoAds == value)
                {
                    return;
                }

                isNoAds = value;
                if (isNoAds)
                {
                    OnAdsRemoved?.Invoke();
                }
            }
        }

        public void ShowRewardedAds(string placement, Action<bool> onCompleted)
        {
            onCompleted?.Invoke(autoCompleteRewardedAds);
        }

        public void ShowBannerAds()
        {
            if (IsNoAds)
            {
                return;
            }
        }

        public void HideBannerAds()
        {
        }
    }
}
