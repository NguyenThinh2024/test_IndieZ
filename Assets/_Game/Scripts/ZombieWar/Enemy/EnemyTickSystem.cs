using System.Collections.Generic;
using UnityEngine;

namespace ZombieWar.Enemy
{
    public sealed class EnemyTickSystem : MonoBehaviour
    {
        [SerializeField] private Transform lodTarget;
        [SerializeField] private EnemyTickLodSettings lodSettings = new EnemyTickLodSettings();
        [SerializeField] private int staggerBucketCount = 8;
        [SerializeField] private float cleanupInterval = 0.5f;

        private readonly List<EnemyTickEntry> entries = new List<EnemyTickEntry>(128);
        private float nextCleanupTime;

        public Transform LodTarget
        {
            get => lodTarget;
            set => lodTarget = value;
        }

        private void OnValidate()
        {
            staggerBucketCount = Mathf.Max(1, staggerBucketCount);
            cleanupInterval = Mathf.Max(0.1f, cleanupInterval);
        }

        public void Register(Enemy zombie)
        {
            if (zombie == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Zombie == zombie)
                {
                    return;
                }
            }

            int bucketIndex = entries.Count % Mathf.Max(1, staggerBucketCount);
            float staggerOffset = bucketIndex * (0.2f / Mathf.Max(1, staggerBucketCount));
            entries.Add(new EnemyTickEntry(zombie, Time.time + staggerOffset));
        }

        public void Unregister(Enemy zombie)
        {
            if (zombie == null)
            {
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Zombie == zombie)
                {
                    entries.RemoveAt(i);
                }
            }
        }

        private void Update()
        {
            if (entries.Count == 0)
            {
                return;
            }

            float now = Time.time;
            if (now >= nextCleanupTime)
            {
                nextCleanupTime = now + cleanupInterval;
                cleanupEntries();
            }

            bool hasLodTarget = lodTarget != null;
            Vector3 targetPosition = hasLodTarget ? lodTarget.position : Vector3.zero;
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < entries.Count; i++)
            {
                EnemyTickEntry entry = entries[i];
                Enemy zombie = entry.Zombie;
                if (zombie == null || !zombie.IsAlive)
                {
                    continue;
                }

                if (now < entry.NextTickTime)
                {
                    continue;
                }

                float statsInterval = zombie.Movement != null ? zombie.Movement.DestinationUpdateInterval : 0.2f;
                float tickInterval = statsInterval;

                if (hasLodTarget && lodSettings != null)
                {
                    float sqrDistance = (zombie.transform.position - targetPosition).sqrMagnitude;
                    tickInterval = lodSettings.GetTickInterval(sqrDistance, statsInterval);
                }

                zombie.Tick(deltaTime);
                entry.NextTickTime = now + tickInterval;
                entries[i] = entry;
            }
        }

        private void cleanupEntries()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Enemy zombie = entries[i].Zombie;
                if (zombie == null || !zombie.gameObject.activeInHierarchy)
                {
                    entries.RemoveAt(i);
                }
            }
        }

        private struct EnemyTickEntry
        {
            public Enemy Zombie;
            public float NextTickTime;

            public EnemyTickEntry(Enemy zombie, float nextTickTime)
            {
                Zombie = zombie;
                NextTickTime = nextTickTime;
            }
        }
    }
}
