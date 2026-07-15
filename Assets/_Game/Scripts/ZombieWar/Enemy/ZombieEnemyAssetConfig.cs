using System;
using UnityEngine;

namespace ZombieWar.Enemy
{
    [Serializable]
    public sealed class ZombieEnemyAssetConfig
    {
        [SerializeField] private string prefabAddress;
        [SerializeField] private string skinMaterialAddress;

        public string PrefabAddress => prefabAddress;
        public string SkinMaterialAddress => skinMaterialAddress;
    }
}
