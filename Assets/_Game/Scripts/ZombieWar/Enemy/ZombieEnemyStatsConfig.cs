using System;
using UnityEngine;

namespace ZombieWar.Enemy
{
    [Serializable]
    public sealed class ZombieEnemyStatsConfig : IZombieStats
    {
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float destinationUpdateInterval = 0.2f;
        [SerializeField] private float animationSpeed = 1.25f;
        [SerializeField] private float locomotionRunBias = 0.9f;
        [SerializeField] private float flankRadius;

        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public float MoveSpeed => Mathf.Max(0f, moveSpeed);
        public float AttackDamage => Mathf.Max(0f, attackDamage);
        public float AttackRange => Mathf.Max(0f, attackRange);
        public float AttackCooldown => Mathf.Max(0.05f, attackCooldown);
        public float DestinationUpdateInterval => Mathf.Max(0.05f, destinationUpdateInterval);
        public float AnimationSpeed => Mathf.Max(0.1f, animationSpeed);
        public float LocomotionRunBias => Mathf.Clamp01(locomotionRunBias);
        public float FlankRadius => Mathf.Max(0f, flankRadius);
    }
}
