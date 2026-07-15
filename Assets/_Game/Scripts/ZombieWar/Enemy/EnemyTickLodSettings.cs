using System;
using UnityEngine;

namespace ZombieWar.Enemy
{
    [Serializable]
    public sealed class EnemyTickLodSettings
    {
        [SerializeField] private float nearDistance = 12f;
        [SerializeField] private float midDistance = 25f;
        [SerializeField] private float nearTickInterval = 0.2f;
        [SerializeField] private float midTickInterval = 0.4f;
        [SerializeField] private float farTickInterval = 0.8f;

        public float GetTickInterval(float sqrDistanceToTarget, float statsDestinationInterval)
        {
            float nearSqr = nearDistance * nearDistance;
            float midSqr = midDistance * midDistance;
            float minNearInterval = Mathf.Max(0.05f, statsDestinationInterval);

            if (sqrDistanceToTarget <= nearSqr)
            {
                return Mathf.Max(minNearInterval, nearTickInterval);
            }

            if (sqrDistanceToTarget <= midSqr)
            {
                return Mathf.Max(0.05f, midTickInterval);
            }

            return Mathf.Max(0.05f, farTickInterval);
        }
    }
}
