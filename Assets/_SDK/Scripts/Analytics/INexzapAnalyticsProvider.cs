namespace Nexzap.Base.Analytics
{
    public interface INexzapAnalyticsProvider
    {
        void LogEvent(string eventName, params (string, object)[] args);

        void LogLevelStart(
            int level,
            float playTime,
            string playType,
            int playIndex,
            int loseIndex);

        void LogLevelEnd(
            int level,
            int playIndex,
            int loseIndex,
            int playDuration,
            float levelProgress,
            int boosterCount,
            int reviveUsed,
            string loseBy,
            string result,
            float playTime);
    }
}
