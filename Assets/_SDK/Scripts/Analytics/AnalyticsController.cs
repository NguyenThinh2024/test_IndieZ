using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if FIREBASE
using Firebase.Analytics;

#endif

#if GAME_ANALYTICS
using GameAnalyticsSDK;
#endif

namespace Nexzap.Base.Analytics
{
    // ================= ENUM =================

    public enum LevelProgressStatus
    {
        Start,
        Fail,
        Complete
    }

    public enum CurrencyType
    {
        Coin,
        Gem,
        Heart,
        Booster
    }

    // ================= CONTROLLER =================

    public class AnalyticsController : MonoSingleton<AnalyticsController>, INexzapAnalyticsProvider
    {
        public override void Init()
        {
            base.Init();
            NexzapAnalytics.SetProvider(this);
#if GAME_ANALYTICS
            GameAnalytics.Initialize();
#endif
        }

        protected override void OnDestroy()
        {
            NexzapAnalytics.ClearProvider(this);
            base.OnDestroy();
        }

        #region ================= APP =================

        public void LogAppOpen()
        {
#if FIREBASE
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAppOpen);
#endif
        }

        #endregion

        #region ================= UTIL =================
        public void LogEvent(
    string eventName,
    params (string, object)[] args
)
        {
            // ================= FIREBASE =================
#if FIREBASE
            try
            {
                LogFirebaseEvent(eventName, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Firebase LogEvent failed: {e.Message}");
            }
#endif

            // ================= GAME_ANALYTICS =================
#if GAME_ANALYTICS
    try
    {
        // Design event thuần (KHÔNG progression)
        if (args != null && args.Length > 0)
        {
            // gộp param thành string đơn giản (debug / segmentation nhẹ)
            string suffix = string.Join(
                "_",
                args.Select(a => $"{a.Item1}:{a.Item2}")
            );

            GameAnalytics.NewDesignEvent(
                $"{eventName}:{suffix}",
                1
            );
        }
        else
        {
            GameAnalytics.NewDesignEvent(
                eventName,
                1
            );
        }
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[Analytics] GA LogEvent failed: {e.Message}");
    }
#endif
        }
        #endregion

        #region ================= LEVEL =================

        public void LogLevelStart(
    int level,
    float playTime,
    string playType,
    int playIndex,
    int loseIndex
)
        {
            // ========== FIREBASE ==========
#if FIREBASE
            try
            {
                // lifecycle
                LogFirebaseEvent(
                    "level_progress",
                    ("status", "start"),
                    ("level", level),
                    ("attempt", 0)
                );

                // level_start
                LogFirebaseEvent(
                    "level_start",
                    ("level", level),
                    ("play_type", playType),
                    ("play_index", playIndex),
                    ("lose_index", loseIndex)
                );

                // legacy
                LogFirebaseEvent($"level_start_{level}");

                if (playTime >= 0f && playTime <= 100f)
                {
                    LogFirebaseEvent($"start_level_{level}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Firebase LogLevelStart failed: {e.Message}");
            }
#endif

            // ========== GAME_ANALYTICS ==========
#if GAME_ANALYTICS
    try
    {
                GameAnalytics.NewProgressionEvent(
            GAProgressionStatus.Start,
            $"level_{level}"
        );
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[Analytics] GA LogLevelStart failed: {e.Message}");
    }
#endif
        }

        public void LogLevelEnd(
    int level,
    int playIndex,
    int loseIndex,
    int playDuration,
    float levelProgress,
    int boosterCount,
    int reviveUsed,
    string loseBy,     // meaningful if lose
    string result,     // win | lose | exit
    float playTime
)
        {
            // ================= FIREBASE =================
#if FIREBASE
            try
            {
                // lifecycle
                LogFirebaseEvent(
                    "level_progress",
                    ("status", result),
                    ("level", level),
                    ("attempt", loseIndex)
                );

                // level_end
                LogFirebaseEvent(
                    "level_end",
                    ("level", level),
                    ("play_index", playIndex),
                    ("lose_index", loseIndex),
                    ("play_duration", playDuration),
                    ("level_progress", levelProgress),
                    ("booster_count", boosterCount),
                    ("revive_used", reviveUsed),
                    ("lose_by", loseBy),
                    ("result", result)
                );

                // legacy
                LogFirebaseEvent($"level_end_{level}_{result}");

                if (playTime <= 100f && result == "win")
                {
                    LogFirebaseEvent($"win_level_{level}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Firebase LogLevelEnd failed: {e.Message}");
            }
#endif

            // ================= GAME_ANALYTICS =================
#if GAME_ANALYTICS
    try
    {
        if (result == "win")
        {
                    GameAnalytics.NewProgressionEvent(
                GAProgressionStatus.Complete,
                $"level_{level}"
            );
        }
        else if (result == "lose")
        {
                    GameAnalytics.NewProgressionEvent(
                GAProgressionStatus.Fail,
                $"level_{level}"
            );
        }
        // exit → KHÔNG log progression
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[Analytics] GA LogLevelEnd failed: {e.Message}");
    }
#endif

            // ================= APPSFLYER =================
#if APPSFLYER
    try
    {
        if (level <= 200 && result == "win")
        {
            AppsFlyerSDK.AppsFlyer.sendEvent(
                $"completed_level_{level}",
                new Dictionary<string, string>()
            );
        }
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[Analytics] AppsFlyer LogLevelEnd failed: {e.Message}");
    }
#endif
        }

        public void LogLevelRevive(
    int level,
    string source   // "ads" | "coin"
)
        {
            // ================= FIREBASE =================
#if FIREBASE
            try
            {
                // main event
                LogFirebaseEvent(
                    "level_revive",
                    ("level", level),
                    ("source", source)
                );

                // legacy
                LogFirebaseEvent($"level_revive_{level}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Firebase LogLevelRevive failed: {e.Message}");
            }
#endif

            // ================= GAME_ANALYTICS =================
#if GAME_ANALYTICS
    try
    {
                // design event để phân tích revive
                GameAnalytics.NewDesignEvent(
            $"level_revive:{source}:level_{level}",
            1
        );
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[Analytics] GA LogLevelRevive failed: {e.Message}");
    }
#endif
        }

        public void LogLevelRestart(
    int level,
    string source = null   // optional: button | lose_popup | setting | auto
)
        {
            // ================= FIREBASE =================
#if FIREBASE
            try
            {
                // main event
                if (string.IsNullOrEmpty(source))
                {
                    LogFirebaseEvent(
                        "level_restart",
                        ("level", level)
                    );
                }
                else
                {
                    LogFirebaseEvent(
                        "level_restart",
                        ("level", level),
                        ("source", source)
                    );
                }

                // legacy
                LogFirebaseEvent($"level_restart_{level}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Firebase LogLevelRestart failed: {e.Message}");
            }
#endif

            // ================= GAME_ANALYTICS =================
#if GAME_ANALYTICS
    try
    {
                // design event để phân tích restart
                GameAnalytics.NewDesignEvent(
            string.IsNullOrEmpty(source)
                ? $"level_restart:level_{level}"
                : $"level_restart:{source}:level_{level}",
            1
        );
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[Analytics] GA LogLevelRestart failed: {e.Message}");
    }
#endif
        }

        #endregion

        #region ================= BOOSTER =================

        public void LogBoosterUse(string boosterId, int level, string sink)
        {
            // ---------- FIREBASE ----------
#if FIREBASE
            LogFirebaseEvent(
                "booster_use",
                ("booster_id", boosterId),
                ("level", level)
            );
#endif

            // ---------- GAME_ANALYTICS ----------
#if GAME_ANALYTICS
            try
            {
                GameAnalytics.NewDesignEvent(
                    $"booster:{boosterId}:use:level_{level}",
                    1
                );

                GameAnalytics.NewResourceEvent(
                   GAResourceFlowType.Sink,
                   CurrencyType.Booster.ToString().ToLower(), // coin / gem / heart
                    1,
                   sink,                           // itemType
                   boosterId                            // itemId
               );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] GA Booster failed: {e.Message}");
            }
#endif
        }

        public void LogBoosterAdd(
                string boosterId,
                int amount,
                string source,   // level_reward | shop | revive | event | daily
                int level)
        {
            // ---------- FIREBASE ----------
#if FIREBASE
            LogFirebaseEvent(
                "booster_add",
                ("booster_id", boosterId),
                ("amount", amount),
                ("source", source),
                ("level", level)
            );
#endif

            // ---------- GAME_ANALYTICS ----------
#if GAME_ANALYTICS
            try
            {
                GameAnalytics.NewDesignEvent(
                    $"booster:{boosterId}:add:{source}",
                    amount
                );

                GameAnalytics.NewResourceEvent(
                    GAResourceFlowType.Source,
                    CurrencyType.Booster.ToString().ToLower(), // coin / gem / heart
                    amount,
                    source,                           // itemType
                    boosterId                            // itemId
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] GA BoosterAdd failed: {e.Message}");
            }
#endif
        }


        #endregion

        #region ================= CURRENCY =================

        public void LogCurrencyEarn(CurrencyType type, int amount, string source)
        {
#if FIREBASE
            LogFirebaseEvent(
                "currency_earn",
                ("currency", type.ToString().ToLower()),
                ("amount", amount),
                ("source", source)
            );
#endif

#if GAME_ANALYTICS
            try
            {
                GameAnalytics.NewResourceEvent(
                    GAResourceFlowType.Source,
                    type.ToString().ToLower(),
                    amount,
                    source,
                    ""
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] GA Currency earn failed: {e.Message}");
            }
#endif
        }

        public void LogCurrencySpend(CurrencyType type, int amount, string sink)
        {
#if FIREBASE
            LogFirebaseEvent(
                "currency_spend",
                ("currency", type.ToString().ToLower()),
                ("amount", amount),
                ("sink", sink)
            );
#endif

#if GAME_ANALYTICS
            try
            {
                GameAnalytics.NewResourceEvent(
                    GAResourceFlowType.Sink,
                    type.ToString().ToLower(),
                    amount,
                    sink,
                    ""
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] GA Currency spend failed: {e.Message}");
            }
#endif
        }

        #endregion

        #region ================= IAP =================

        /// <summary>
        /// Log IAP purchase success
        /// </summary>
        public void LogIAP(
            string productId,
            double price,
            string currency,
            int level
        )
        {
            // ---------- FIREBASE ----------
#if FIREBASE
            try
            {
                FirebaseAnalytics.LogEvent(
                    FirebaseAnalytics.EventPurchase,
                    new Parameter[]
                    {
                        new Parameter("item_id", productId),
                        new Parameter("value", price),
                        new Parameter("currency", currency),
                        new Parameter("level", level)
                    }
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Firebase IAP failed: {e.Message}");
            }
#endif

            // ---------- GAME_ANALYTICS ----------
#if GAME_ANALYTICS
            try
            {
                // GA yêu cầu price * 100 (int)
                int cents = Mathf.RoundToInt((float)(price * 100));

                GameAnalytics.NewBusinessEvent(
                    currency,
                    cents,
                    "iap",
                    productId,
                    $"level_{level}"
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] GA IAP failed: {e.Message}");
            }
#endif
        }

        #endregion

        #region ================= USER PROPERTY =================

        public void LogUserLevelProperty(int level)
        {
            SetUserProperty("current_level", level);
        }

        public void LogUserCoinProperty(int coin)
        {
            SetUserProperty("coin_balance", coin);
        }

        public void LogUserHeartProperty(int heart)
        {
            SetUserProperty("heart_balance", heart);
        }

        private void SetUserProperty(string key, object value)
        {
            if (value == null) return;

            string strValue = value.ToString();

#if FIREBASE
            try
            {
                FirebaseAnalytics.SetUserProperty(key, strValue);
            }
            catch { }
#endif
        }

        public void LogUserProgress(int currentLevel)
        {
#if FIREBASE
            try
            {
                LogUserLevelProperty((int)currentLevel);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Set current_level failed: {e.Message}");
            }
#endif

#if GAME_ANALYTICS
    try
    {
            // dùng dimension cho segmentation
            GameAnalytics.SetCustomDimension01(
            $"level_{currentLevel}"
        );
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[Analytics] GA set dimension failed: {e.Message}");
    }
#endif
        }

        #endregion

        #region ================= FIREBASE CORE =================

#if FIREBASE
        private void LogFirebaseEvent(
            string eventName,
            params (string, object)[] args
        )
        {
            try
            {
                List<Parameter> parameters = new();

                foreach (var arg in args)
                {
                    if (arg.Item2 is int i) parameters.Add(new Parameter(arg.Item1, i));
                    else if (arg.Item2 is long l) parameters.Add(new Parameter(arg.Item1, l));
                    else if (arg.Item2 is float f) parameters.Add(new Parameter(arg.Item1, f));
                    else if (arg.Item2 is double d) parameters.Add(new Parameter(arg.Item1, d));
                    else parameters.Add(new Parameter(arg.Item1, arg.Item2.ToString()));
                }

                FirebaseAnalytics.LogEvent(eventName, parameters.ToArray());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Firebase event failed: {e.Message}");
            }
        }
#endif

        #endregion
    }
}
