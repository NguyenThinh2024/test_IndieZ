using UnityEngine;

namespace ZombieWar.Enemy
{
    [CreateAssetMenu(fileName = "ZombieData", menuName = "Zombie War/Enemy/Zombie Data")]
    public sealed class ZombieData : ScriptableObject, IZombieStats
    {
        [SerializeField] private float maxHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackRange;
        [SerializeField] private float attackCooldown;
        [SerializeField] private float destinationUpdateInterval;
        [SerializeField] private float animationSpeed = 1.25f;
        [SerializeField] private float locomotionRunBias = 0.9f;
        [SerializeField] private float flankRadius;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public float DestinationUpdateInterval => destinationUpdateInterval;
        public float AnimationSpeed => Mathf.Max(0.1f, animationSpeed);
        public float LocomotionRunBias => Mathf.Clamp01(locomotionRunBias);
        public float FlankRadius => Mathf.Max(0f, flankRadius);
    }
}
