using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Enemy
{
    public sealed class ZombieAttack : MonoBehaviour
    {
        [SerializeField] private ZombieMovement movement;
        [SerializeField] private ZombieHealth health;
        [SerializeField] private ZombieAnimation zombieAnimation;

        private IZombieStats stats;
        private IDamageable targetDamageable;
        private GameObject targetObject;
        private float nextAttackTime;
        private bool usesCentralTick;
        private bool isHoldingForMelee;

        public void SetCentralTick(bool enabled)
        {
            usesCentralTick = enabled;
        }

        public void Initialize(IZombieStats zombieStats, IDamageable target, GameObject targetGameObject)
        {
            OnSpawn(zombieStats, target, targetGameObject);
        }

        public void OnSpawn(IZombieStats zombieStats, IDamageable target, GameObject targetGameObject)
        {
            stats = zombieStats;
            targetDamageable = target;
            targetObject = targetGameObject;
            nextAttackTime = 0f;
            isHoldingForMelee = false;
        }

        public void OnDespawn()
        {
            usesCentralTick = false;
            stats = null;
            targetDamageable = null;
            targetObject = null;
            nextAttackTime = 0f;
            isHoldingForMelee = false;
        }

        private void Update()
        {
            if (usesCentralTick)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (stats == null || targetDamageable == null || !targetDamageable.IsAlive || health == null || !health.IsAlive)
            {
                return;
            }

            if (movement == null)
            {
                return;
            }

            float attackRange = stats.AttackRange;
            if (movement.DistanceToTarget > attackRange)
            {
                if (isHoldingForMelee)
                {
                    movement.ResumeChase();
                    isHoldingForMelee = false;
                }

                return;
            }

            if (!isHoldingForMelee)
            {
                movement.Stop();
                isHoldingForMelee = true;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + stats.AttackCooldown;
            zombieAnimation?.PlayAttack();
            Vector3 direction = targetObject != null
                ? targetObject.transform.position - transform.position
                : transform.forward;
            DamageInfo damageInfo = new DamageInfo(
                stats.AttackDamage,
                transform.position + Vector3.up,
                direction,
                gameObject,
                DamageType.Melee);
            targetDamageable.TakeDamage(damageInfo);
        }
    }
}
