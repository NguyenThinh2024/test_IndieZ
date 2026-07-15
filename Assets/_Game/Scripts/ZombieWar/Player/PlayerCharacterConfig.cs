using System;
using UnityEngine;

namespace ZombieWar.Player
{
    [Serializable]
    public sealed class PlayerCharacterConfig
    {
        [SerializeField] private string id = "soldier";
        [SerializeField] private string displayName = "Soldier";
        [SerializeField] private PlayerCharacterAssetConfig character = new PlayerCharacterAssetConfig();
        [SerializeField] private PlayerCharacterStatsConfig stats = new PlayerCharacterStatsConfig();
        [SerializeField] private PlayerCharacterAnimationConfig animation = new PlayerCharacterAnimationConfig();

        public string Id => id;
        public string DisplayName => displayName;
        public PlayerCharacterAssetConfig Character => character;
        public PlayerCharacterStatsConfig Stats => stats;
        public PlayerCharacterAnimationConfig Animation => animation;
    }
}
