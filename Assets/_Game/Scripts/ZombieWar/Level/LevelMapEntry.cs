using System;
using UnityEngine;

namespace ZombieWar.Level
{
    [Serializable]
    public sealed class LevelMapEntry
    {
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private string mapAddress;
        [SerializeField] private string displayName;
        [SerializeField] private LevelWaveConfig waveConfig;

        public int LevelNumber => levelNumber;
        public string MapAddress => mapAddress;
        public string DisplayName => displayName;
        public LevelWaveConfig WaveConfig => waveConfig;

        public bool HasAddress => !string.IsNullOrWhiteSpace(mapAddress);
        public bool HasWaveConfig => waveConfig != null;
    }
}
