using System;
using TBN;
using UnityEngine;
using ThinhPooling;
using ZombieWar.Core;

namespace ZombieWar.Enemy
{
    public sealed class ZombieHealth : MonoBehaviour, IDamageable, IPoolReleaseListener
    {
        [SerializeField] private Health health = new Health();
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private GameObject deathVfxPrefab;
        [SerializeField] private float recycleDelay = 1.2f;

        private PooledInstance pooledInstance;

        public event Action<DamageInfo> Hit;
        public event Action Died;

        event Action IPoolReleaseListener.ReleaseRequested
        {
            add => Died += value;
            remove => Died -= value;
        }

        float IPoolReleaseListener.ReleaseDelay => recycleDelay;

        public bool IsAlive => health.IsAlive;
        public bool UsesThinhPooling => pooledInstance != null;

        private void Awake()
        {
            pooledInstance = GetComponent<PooledInstance>();
            health.Depleted += Die;
        }

        private void OnDestroy()
        {
            health.Depleted -= Die;
        }

        private void OnEnable()
        {
            health.Initialize();
        }

        public void Initialize(IZombieStats stats)
        {
            OnSpawn(stats);
        }

        public void OnSpawn(IZombieStats stats)
        {
            if (stats != null)
            {
                health.SetMaxHealth(stats.MaxHealth, true);
            }
            else
            {
                health.Initialize();
            }
        }

        public void OnDespawn()
        {
            health.Initialize();
        }

        public void TakeDamage(in DamageInfo damageInfo)
        {
            if (!health.ApplyDamage(damageInfo.Damage))
            {
                return;
            }

            Hit?.Invoke(damageInfo);
            PooledVfx.Spawn(hitVfxPrefab, damageInfo.HitPoint, Quaternion.LookRotation(-damageInfo.HitDirection), 1.5f);
        }

        private void Die()
        {
            Died?.Invoke();
            PooledVfx.Spawn(deathVfxPrefab, transform.position, transform.rotation, 2f);

            if (UsesThinhPooling)
            {
                return;
            }

            gameObject.Recycle(recycleDelay);
        }
    }
}
