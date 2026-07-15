using System;
using UnityEngine;

namespace ZombieWar.Enemy
{
    [Serializable]
    public sealed class ZombieEnemyConfig
    {
        [SerializeField] private string id = "zombie";
        [SerializeField] private string displayName = "Zombie";
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private ZombieEnemyAssetConfig character = new ZombieEnemyAssetConfig();
        [SerializeField] private ZombieEnemyStatsConfig stats = new ZombieEnemyStatsConfig();
        [SerializeField] private ZombieEnemyAudioConfig audio = new ZombieEnemyAudioConfig();

        public string Id => id;
        public string DisplayName => displayName;
        public int SpawnCount => Mathf.Max(0, spawnCount);
        public ZombieEnemyAssetConfig Character => character;
        public ZombieEnemyStatsConfig Stats => stats;
        public ZombieEnemyAudioConfig Audio => audio;
    }
}
