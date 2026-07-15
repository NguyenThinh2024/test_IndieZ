using System;
using UnityEngine;

namespace Nexzap.Base.Analytics
{
    public static class NexzapAnalytics
    {
        private const string LogPrefix = "[Nexzap.Analytics]";

        private static INexzapAnalyticsProvider provider;
        private static float sessionStartTime;
        private static bool hasStarted;
        private static bool hasFinished;

        public static void SetProvider(INexzapAnalyticsProvider analyticsProvider)
        {
            provider = analyticsProvider;
        }

        public static void ClearProvider(INexzapAnalyticsProvider analyticsProvider)
        {
            if (ReferenceEquals(provider, analyticsProvider))
            {
                provider = null;
            }
        }

        public static void ReportGameStarted(
            int level,
            string playType = "normal",
            int playIndex = 1,
            int loseIndex = 0)
        {
            level = Mathf.Max(1, level);
            sessionStartTime = Time.realtimeSinceStartup;
            hasStarted = true;
            hasFinished = false;

            Debug.Log($"{LogPrefix} GameStarted | level={level} | playType={playType} | playIndex={playIndex} | loseIndex={loseIndex}");

            provider?.LogEvent(
                "nexzap_game_started",
                ("level", level),
                ("play_type", playType),
                ("play_index", playIndex),
                ("lose_index", loseIndex));
            provider?.LogLevelStart(level, 0f, playType, playIndex, loseIndex);
        }

        public static void ReportGameFinished(
            bool levelComplete,
            float score,
            int level,
            string loseBy = "",
            int playIndex = 1,
            int loseIndex = 0,
            int boosterCount = 0,
            int reviveUsed = 0)
        {
            if (hasFinished)
            {
                return;
            }

            level = Mathf.Max(1, level);
            string result = levelComplete ? "win" : "lose";
            int playDuration = hasStarted
                ? Mathf.Max(0, Mathf.RoundToInt(Time.realtimeSinceStartup - sessionStartTime))
                : 0;

            hasFinished = true;

            Debug.Log($"{LogPrefix} GameFinished | result={result} | level={level} | score={score} | duration={playDuration} | loseBy={loseBy}");

            provider?.LogEvent(
                "nexzap_game_finished",
                ("result", result),
                ("level", level),
                ("score", score),
                ("play_duration", playDuration),
                ("lose_by", loseBy));
            provider?.LogLevelEnd(
                level,
                playIndex,
                loseIndex,
                playDuration,
                levelComplete ? 100f : 0f,
                boosterCount,
                reviveUsed,
                loseBy,
                result,
                playDuration);
        }
    }
}
