using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ZombieWar.Enemy;

namespace ZombieWar.Level
{
    [Serializable]
    public sealed class WaveData
    {
        // Delay after this wave unlocks before first spawn (wave 0 unlocks at t=0).
        [SerializeField] private float startTime;
        [SerializeField] private float spawnInterval = 1f;
        [SerializeField] private int spawnCount = 10;
        [SerializeField] private int maxAlive = 12;

        [SerializeField] private string displayName;
        [SerializeField] private string announceSubtitle;
        [SerializeField] private bool isBoss;
        [SerializeField] private bool announceEnabled = true;
        [SerializeField] private float announceLeadSeconds = 2f;

        [SerializeField] private AssetReferenceT<TextAsset> zombieConfigReference;
        [SerializeField] private ZombieWar.Enemy.Enemy zombiePrefab;
        [SerializeField] private ZombieData zombieDataOverride;

        public float StartTime => startTime;
        public float SpawnInterval => spawnInterval;
        public int SpawnCount => Mathf.Max(0, spawnCount);
        public int MaxAlive => maxAlive;
        public string DisplayName => displayName;
        public string AnnounceSubtitle => announceSubtitle;
        public bool IsBoss => isBoss;
        public bool AnnounceEnabled => announceEnabled;
        public float AnnounceLeadSeconds => Mathf.Max(0f, announceLeadSeconds);
        public AssetReferenceT<TextAsset> ZombieConfigReference => zombieConfigReference;
        public ZombieWar.Enemy.Enemy ZombiePrefab => zombiePrefab;
        public ZombieData ZombieDataOverride => zombieDataOverride;

        public float GetAnnounceTime()
        {
            return Mathf.Max(0f, startTime - AnnounceLeadSeconds);
        }
    }
}
