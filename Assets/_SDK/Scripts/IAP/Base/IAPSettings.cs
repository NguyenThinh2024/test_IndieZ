#if IAP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;


namespace Thinh.Base.IAP
{
    [Serializable]
    public class IAPSettings
    {
        public Dictionary<IAPProductType, ProductType> Products =
            new Dictionary<IAPProductType, ProductType>
            {
            // ===== BUNDLE / LIMITED =====
            { IAPProductType.Limited,        ProductType.Consumable },
            { IAPProductType.Bundle1,        ProductType.Consumable },
            { IAPProductType.Bundle2,        ProductType.Consumable },
            { IAPProductType.Bundle3,        ProductType.Consumable },
            { IAPProductType.Special,        ProductType.Consumable },

            // ===== COIN PACK =====
            { IAPProductType.CoinPack1,      ProductType.Consumable },
            { IAPProductType.CoinPack2,      ProductType.Consumable },
            { IAPProductType.CoinPack3,      ProductType.Consumable },
            { IAPProductType.CoinPack4,      ProductType.Consumable },
            { IAPProductType.CoinPack5,      ProductType.Consumable },
            { IAPProductType.CoinPack6,      ProductType.Consumable },

            // ===== ADS TIME PACK (NO ADS TẠM) =====
            { IAPProductType.AdsPack1,       ProductType.Consumable },
            { IAPProductType.AdsPack2,       ProductType.Consumable },
            { IAPProductType.AdsPack3,       ProductType.Consumable },

            // ===== NO ADS VĨNH VIỄN =====
            { IAPProductType.NoAds,          ProductType.NonConsumable },
            { IAPProductType.NoAdsBundle,    ProductType.NonConsumable },
            };
    }


    public enum IAPProductType
    {
        // Limited / Special
        Limited,
        Special,

        // Bundle packs
        Bundle1,
        Bundle2,
        Bundle3,

        // Coin Packs (6)
        CoinPack1,
        CoinPack2,
        CoinPack3,
        CoinPack4,
        CoinPack5,
        CoinPack6,

        // Ads Packs (3)
        AdsPack1,
        AdsPack2,
        AdsPack3,

        // Testing
        Fail,

        NoAds,
        NoAdsBundle
    }

    public static class IAPIdMap
    {
        public static string GetID(IAPProductType type)
        {
            return type switch
            {
                IAPProductType.Limited => "limited",
                IAPProductType.Special => "special",

                IAPProductType.Bundle1 => "bundle1",
                IAPProductType.Bundle2 => "bundle2",
                IAPProductType.Bundle3 => "bundle3",

                IAPProductType.CoinPack1 => "coin_pack_1",
                IAPProductType.CoinPack2 => "coin_pack_2",
                IAPProductType.CoinPack3 => "coin_pack_3",
                IAPProductType.CoinPack4 => "coin_pack_4",
                IAPProductType.CoinPack5 => "coin_pack_5",
                IAPProductType.CoinPack6 => "coin_pack_6",

                IAPProductType.AdsPack1 => "ads_pack_1",
                IAPProductType.AdsPack2 => "ads_pack_2",
                IAPProductType.AdsPack3 => "ads_pack_3",

                IAPProductType.Fail => "fail",
                IAPProductType.NoAds => "no_ads",
                IAPProductType.NoAdsBundle => "no_ads_bundle",
                _ => null
            };
        }
    }
}
#endif