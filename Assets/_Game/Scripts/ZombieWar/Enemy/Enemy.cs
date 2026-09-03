using UnityEngine;
using UnityEngine.Serialization;
using ThinhPooling;
using ZombieWar.Core;

namespace ZombieWar.Enemy
{
    public sealed class Enemy : MonoBehaviour, IPoolable
    {
        [SerializeField] private ZombieHealth health;
        [SerializeField] private ZombieMovement movement;
        [SerializeField] private ZombieAttack attack;
        [FormerlySerializedAs("animation")]
        [SerializeField] private ZombieAnimation zombieAnimation;
        [SerializeField] private ZombieDissolve dissolve;
        [SerializeField] private ZombieAudio zombieAudio;

        public ZombieHealth Health => health;
        public ZombieMovement Movement => movement;
        public ZombieAudio Audio => zombieAudio;
        public bool IsAlive => health != null && health.IsAlive;

        private EnemyTickSystem registeredTickSystem;

        public void Initialize(IZombieStats zombieStats, Transform targetTransform, IDamageable targetDamageable)
        {
            Initialize(zombieStats, targetTransform, targetDamageable, null);
        }

        public void Initialize(
            IZombieStats zombieStats,
            Transform targetTransform,
            IDamageable targetDamageable,
            EnemyTickSystem tickSystem)
        {
            OnSpawn(zombieStats, targetTransform, targetDamageable);
            registerTickSystem(tickSystem);
        }

        public void Tick(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            movement?.Tick(deltaTime);
            attack?.Tick(deltaTime);
            zombieAnimation?.Tick(deltaTime);
            zombieAudio?.Tick(deltaTime);
        }

        private void registerTickSystem(EnemyTickSystem tickSystem)
        {
            registeredTickSystem?.Unregister(this);
            registeredTickSystem = tickSystem;

            bool useCentralTick = tickSystem != null;
            movement?.SetCentralTick(useCentralTick);
            attack?.SetCentralTick(useCentralTick);
            zombieAnimation?.SetCentralTick(useCentralTick);
            ensureAudio()?.SetCentralTick(useCentralTick);

            registeredTickSystem?.Register(this);
        }

        public void OnSpawn(IZombieStats zombieStats, Transform targetTransform, IDamageable targetDamageable)
        {
            health?.OnSpawn(zombieStats);
            movement?.OnSpawn(zombieStats, targetTransform);

            if (attack != null)
            {
                GameObject targetObject = targetTransform != null ? targetTransform.gameObject : null;
                attack.OnSpawn(zombieStats, targetDamageable, targetObject);
            }

            zombieAnimation?.OnSpawn(zombieStats);
            dissolve?.OnSpawn();
            ensureAudio()?.OnSpawn();
        }

        public void OnDespawn()
        {
            registeredTickSystem?.Unregister(this);
            registeredTickSystem = null;

            zombieAudio?.OnDespawn();
            dissolve?.OnDespawn();
            zombieAnimation?.OnDespawn();
            attack?.OnDespawn();
            movement?.OnDespawn();
            health?.OnDespawn();

            gameObject.SetActive(false);
        }

        private ZombieAudio ensureAudio()
        {
            if (zombieAudio != null)
            {
                return zombieAudio;
            }

            zombieAudio = GetComponent<ZombieAudio>();
            if (zombieAudio == null)
            {
                zombieAudio = gameObject.AddComponent<ZombieAudio>();
            }

            return zombieAudio;
        }
    }
}
