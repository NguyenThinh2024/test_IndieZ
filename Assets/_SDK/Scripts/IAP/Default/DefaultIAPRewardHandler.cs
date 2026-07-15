#if IAP
using Cysharp.Threading.Tasks;
using Nexzap.Base.Ads;
using Nexzap.Base.Data;
using Nexzap.Base.Analytics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if APPSFLYER
using AppsFlyerSDK;
#endif

namespace Nexzap.Base.IAP
{
    public class DefaultIAPRewardHandler : BaseIAPRewardHandler
    {
        protected override async void HandleReward(IAPProductType type)
        {
            switch (type)
            {
                // --------------------------------------------------
                // COIN PACKS
                // --------------------------------------------------
                case IAPProductType.CoinPack1:
                    TrackIAPReward(type, "coin", 1000);
                    await PlayAnimCoins(1000);
                    break;
                case IAPProductType.CoinPack2:
                    TrackIAPReward(type, "coin", 5000);
                    await PlayAnimCoins(5000);
                    break;
                case IAPProductType.CoinPack3:
                    TrackIAPReward(type, "coin", 10000);
                    await PlayAnimCoins(10000);
                    break;
                case IAPProductType.CoinPack4:
                    TrackIAPReward(type, "coin", 25000);
                    await PlayAnimCoins(25000);
                    break;
                case IAPProductType.CoinPack5:
                    TrackIAPReward(type, "coin", 50000);
                    await PlayAnimCoins(50000);
                    break;
                case IAPProductType.CoinPack6:
                    TrackIAPReward(type, "coin", 100000);
                    await PlayAnimCoins(100000);
                    break;

                // --------------------------------------------------
                // ADS PACKS
                // --------------------------------------------------
                case IAPProductType.AdsPack1:
                    TrackIAPReward(type, "ads_ticket", 3);
                    await PlayAnimAdsTicket(3);
                    break;
                case IAPProductType.AdsPack2:
                    TrackIAPReward(type, "ads_ticket", 6);
                    await PlayAnimAdsTicket(6);
                    break;
                case IAPProductType.AdsPack3:
                    TrackIAPReward(type, "ads_ticket", 9);
                    await PlayAnimAdsTicket(9);
                    break;

                // --------------------------------------------------
                // BUNDLE 1 – Big Bundle
                // --------------------------------------------------
                case IAPProductType.Bundle1:
                    TrackIAPReward(type, "coin", 2500);
                    TrackIAPReward(type, "booster_bundle", 2);
                    TrackIAPReward(type, "ads_ticket", 2);
                    await PlayAnimCoins(2500);
                    await PlayBundleBoosters(2);
                    await PlayAnimAdsTicket(2);
                    break;

                // --------------------------------------------------
                // BUNDLE 2 – Great Bundle
                // --------------------------------------------------
                case IAPProductType.Bundle2:
                    TrackIAPReward(type, "coin", 5500);
                    TrackIAPReward(type, "booster_bundle", 3);
                    TrackIAPReward(type, "ads_ticket", 3);
                    TrackIAPReward(type, "infinite_life_minutes", 180);
                    await PlayAnimCoins(5500);
                    await PlayBundleBoosters(3);
                    await PlayAnimAdsTicket(3);
                    await PlayAnimLife(180); // 3 giờ = 180 phút
                    break;

                // --------------------------------------------------
                // BUNDLE 3 – Ultra Bundle
                // --------------------------------------------------
                case IAPProductType.Bundle3:
                    TrackIAPReward(type, "coin", 12000);
                    TrackIAPReward(type, "booster_bundle", 6);
                    TrackIAPReward(type, "ads_ticket", 6);
                    TrackIAPReward(type, "infinite_life_minutes", 360);
                    await PlayAnimCoins(12000);
                    await PlayBundleBoosters(6);
                    await PlayAnimAdsTicket(6);
                    await PlayAnimLife(360); // 6 giờ = 360 phút
                    break;

                // --------------------------------------------------
                // LIMITED PACK
                // --------------------------------------------------
                case IAPProductType.Limited:
                    TrackIAPReward(type, "coin", 4000);
                    TrackIAPReward(type, "infinite_life_minutes", 120);
                    TrackIAPReward(type, "booster_bundle", 2);
                    TrackIAPReward(type, "ads_ticket", 2);
                    await PlayAnimCoins(4000);
                    await PlayAnimLife(120); // 2 giờ = 120 phút
                    await PlayBundleBoosters(2);
                    await PlayAnimAdsTicket(2);
                    break;

                // --------------------------------------------------
                // SPECIAL PACK
                // --------------------------------------------------
                case IAPProductType.Special:
                    TrackIAPReward(type, "coin", 1000);
                    TrackIAPReward(type, "infinite_life_minutes", 120);
                    TrackIAPReward(type, "booster_bundle", 2);
                    TrackIAPReward(type, "ads_ticket", 2);
                    await PlayAnimCoins(1000);
                    await PlayAnimLife(120); // 2 giờ = 120 phút
                    await PlayBundleBoosters(2);
                    await PlayAnimAdsTicket(2);
                    break;

                // --------------------------------------------------
                // No Ads 
                // --------------------------------------------------
                case IAPProductType.NoAds:
                    RemoveAds(); //remove ads
                    break;

                // --------------------------------------------------
                // BUNDLE 2 – Great Bundle
                // --------------------------------------------------
                case IAPProductType.NoAdsBundle:
                    TrackIAPReward(type, "coin", 2000);
                    TrackIAPReward(type, "booster_bundle", 2);
                    TrackIAPReward(type, "ads_ticket", 2);
                    TrackIAPReward(type, "infinite_life_minutes", 120); 
                    TrackIAPReward(type, "remove_ads", 1);
                    RemoveAds(); //remove ads
                    await PlayAnimCoins(2000);
                    await PlayBundleBoosters(2);
                    await PlayAnimAdsTicket(2);
                    await PlayAnimLife(120); //2 giờ = 120 phút                  
                    break;

                default:
                    Debug.LogWarning($"[Reward] Missing reward logic for: {type}");
                    break;
            }
        }

        // ==========================================================
        // BOOSTER / COIN / ADSTICKET / LIFE / REMOVE ADS ANIMATIONS
        // ==========================================================

        /// <summary>
        /// Tạo điểm nguồn (src) từ giữa màn hình cho animation IAP
        /// </summary>
        private RectTransform GetCenterScreenSource(Canvas canvas)
        {
            GameObject go = new GameObject("IAPRewardSource", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            
            // Đặt ở giữa màn hình
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            
            // Tự động xóa sau một thời gian
            GameObject.Destroy(go, 5f);
            
            return rt;
        }

        private async UniTask PlayBundleBoosters(int amount)
        {

            
            await PlayAnimBooster(eBooster.Pack, amount);
            await PlayAnimBooster(eBooster.Freeze, amount);
            await PlayAnimBooster(eBooster.Shuffle, amount);
        }

        private async UniTask PlayAnimCoins(int value)
        {
            Canvas canvas = ResourceManager.Instance.GetComponent<Canvas>();
            RectTransform src = GetCenterScreenSource(canvas);
            RectTransform dst = ResourceManager.Instance.UIResource.coinImg.rectTransform;
            
            // Giới hạn số lượng coin trong animation (tối đa 20 coin)
            int animCount = Mathf.Min(value, 20);
            RewardData animData = RewardAnimation.CreateReward(eItemType.Currency, animCount);
            
            // Tạo reward data thực tế với số lượng đúng
            RewardData realReward = RewardAnimation.CreateReward(eItemType.Currency, value);

            await RewardUtility.PlayRewardAnimation(realReward, src, canvas);
            
            // await RewardAnimation.Play(animData, src, dst, canvas, (reward) =>
            // {
            //     // Grant reward với số lượng thực tế
            //     RewardUtility.GainReward(realReward);
            // });
        }

        private async UniTask PlayAnimBooster(eBooster booster, int amount)
        {
            Canvas canvas = ResourceManager.Instance.GetComponent<Canvas>();
            RectTransform src = GetCenterScreenSource(canvas);
            RectTransform dst = ResourceManager.Instance.UIAvatarBtn.button.image.rectTransform;
            
            RewardData data = RewardAnimation.CreateBoosterReward(booster, amount);
            // await RewardAnimation.Play(data, src, dst, canvas, (reward) =>
            // {
            //     RewardUtility.GainReward(reward);
            // });

            await RewardUtility.PlayRewardAnimation(data, src, canvas);
        }

        private async UniTask PlayAnimAdsTicket(int amount)
        {
            Canvas canvas = ResourceManager.Instance.GetComponent<Canvas>();
            RectTransform src = GetCenterScreenSource(canvas);
            // AdsTicket bay vào avatar giống như các booster
            RectTransform dst = ResourceManager.Instance.UIAvatarBtn.button.image.rectTransform;
            
            RewardData data = RewardAnimation.CreateReward(eItemType.AdsTicket, amount);
            // await RewardAnimation.Play(data, src, dst, canvas, (reward) =>
            // {
            //     RewardUtility.GainReward(reward);
            // });

            await RewardUtility.PlayRewardAnimation(data, src, canvas);
        }

        private async UniTask PlayAnimLife(int minus)
        {
            Canvas canvas = ResourceManager.Instance.GetComponent<Canvas>();
            RectTransform src = GetCenterScreenSource(canvas);
            RectTransform dst = ResourceManager.Instance.UIResource.lifeImg.rectTransform;
            
            RewardData data = RewardAnimation.CreateReward(eItemType.InfiniteLife, minus);
            // await RewardAnimation.Play(data, src, dst, canvas, (reward) =>
            // {
            //     RewardUtility.GainReward(reward);
            // });
            await RewardUtility.PlayRewardAnimation(data, src, canvas);
        }

        // ==========================================================
        // REMOVE ADS (không có animation, chỉ cần set flag)
        // ==========================================================

        private void RemoveAds()
        {
            AdsController.Instance.IsNoAds = true;
        }

        /// <summary>
        /// Tracking reward sau khi mua IAP: ghi lại loại sản phẩm, loại reward và amount.
        /// </summary>
        private void TrackIAPReward(IAPProductType productType, string rewardType, int amount)
        {
            AnalyticsController.Instance.LogEvent(
                "iap_reward",
                ("product", productType.ToString()),
                ("reward_type", rewardType),
                ("amount", amount)
            );
        }
    }
}
#endif