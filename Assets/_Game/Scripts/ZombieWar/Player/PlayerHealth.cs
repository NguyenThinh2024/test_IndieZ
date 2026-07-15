using System;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Player
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private Health health = new Health();
        [SerializeField] private GameObject damageVfxPrefab;
        [SerializeField] private Transform damageVfxPoint;

        public event Action<float> HealthNormalizedChanged;
        public event Action Damaged;
        public event Action Died;

        public bool IsAlive => health.IsAlive;

        private void Awake()
        {
            health.Initialize();
            health.Changed += OnHealthChanged;
            health.Depleted += OnHealthDepleted;
        }

        private void OnDestroy()
        {
            health.Changed -= OnHealthChanged;
            health.Depleted -= OnHealthDepleted;
        }

        public void TakeDamage(in DamageInfo damageInfo)
        {
            if (!health.ApplyDamage(damageInfo.Damage))
            {
                return;
            }

            Damaged?.Invoke();

            Vector3 spawnPosition = damageInfo.HitPoint;
            if (spawnPosition.sqrMagnitude < 0.0001f)
            {
                spawnPosition = damageVfxPoint != null ? damageVfxPoint.position : transform.position;
            }

            Quaternion spawnRotation = Quaternion.LookRotation(-damageInfo.HitDirection);
            PooledVfx.Spawn(damageVfxPrefab, spawnPosition, spawnRotation, 1.5f);
        }

        private void OnHealthChanged(float current, float max)
        {
            HealthNormalizedChanged?.Invoke(max > 0f ? Mathf.Clamp01(current / max) : 0f);
        }

        private void OnHealthDepleted()
        {
            Died?.Invoke();
        }
    }
}
