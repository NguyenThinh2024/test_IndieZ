namespace ZombieWar.Level
{
    /// <summary>
    /// Immutable UI payload when a wave is announced.
    /// Raised by WaveManager; consumed by WaveAnnouncePresenter.
    /// </summary>
    public readonly struct WaveAnnounceInfo
    {
        public readonly int WaveIndex;
        public readonly int WaveNumber;
        public readonly string DisplayName;
        public readonly string AnnounceSubtitle;
        public readonly bool IsBoss;
        public readonly float StartTime;

        public WaveAnnounceInfo(
            int waveIndex,
            string displayName,
            bool isBoss,
            float startTime,
            string announceSubtitle = null)
        {
            WaveIndex = waveIndex;
            WaveNumber = waveIndex + 1;
            DisplayName = displayName;
            AnnounceSubtitle = announceSubtitle;
            IsBoss = isBoss;
            StartTime = startTime;
        }

        public string ResolveTitle()
        {
            if (IsBoss)
            {
                return "BOSS WARNING";
            }

            return string.IsNullOrWhiteSpace(DisplayName)
                ? $"WAVE {WaveNumber}"
                : DisplayName;
        }

        public string ResolveSubtitle()
        {
            if (!string.IsNullOrWhiteSpace(AnnounceSubtitle))
            {
                return AnnounceSubtitle;
            }

            if (IsBoss)
            {
                return string.IsNullOrWhiteSpace(DisplayName)
                    ? "Prepare for the boss!"
                    : $"{DisplayName} incoming!";
            }

            return "Incoming!";
        }
    }
}
