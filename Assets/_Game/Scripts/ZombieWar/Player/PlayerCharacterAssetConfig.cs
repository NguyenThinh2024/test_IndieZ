using System;
using UnityEngine;

namespace ZombieWar.Player
{
    [Serializable]
    public sealed class PlayerCharacterAssetConfig
    {
        [SerializeField] private string prefabAddress;

        public string PrefabAddress => prefabAddress;
    }
}
