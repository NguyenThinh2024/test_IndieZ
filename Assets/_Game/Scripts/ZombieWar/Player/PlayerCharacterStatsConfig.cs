using System;
using UnityEngine;

namespace ZombieWar.Player
{
    [Serializable]
    public sealed class PlayerCharacterStatsConfig
    {
        [SerializeField] private float moveSpeed = 5f;

        public float MoveSpeed => Mathf.Max(0f, moveSpeed);
    }
}
