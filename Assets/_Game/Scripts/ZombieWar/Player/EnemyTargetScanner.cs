using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Player
{
    // Owns in-range enemies from EnemyDetectZone triggers and exposes the nearest alive aim target.
    public sealed class EnemyTargetScanner : MonoBehaviour
    {
        private const int OverlapBufferSize = 64;

        [SerializeField] private Transform origin;
        [SerializeField] private LayerMask enemyMask;
        [SerializeField] private DamageableHitboxResolver hitboxResolver;
        [SerializeField] private EnemyDetectZone detectZone;
        [SerializeField] private float retargetInterval = 0.12f;
        [SerializeField] private float overlapRescanInterval = 0.25f;

        private readonly Dictionary<Collider, DamageableHitbox> hitboxByCollider = new Dictionary<Collider, DamageableHitbox>(128);
        private readonly HashSet<DamageableHitbox> detectedHitboxes = new HashSet<DamageableHitbox>();
        private readonly List<DamageableHitbox> pruneBuffer = new List<DamageableHitbox>(32);
        private readonly Collider[] overlapBuffer = new Collider[OverlapBufferSize];
        private float nextRetargetTime;
        private float nextOverlapRescanTime;

        public Transform CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        private void Awake()
        {
            bindDetectZone();
        }

        private void OnEnable()
        {
            // Zombies already inside the trigger at spawn never get OnTriggerEnter.
            nextOverlapRescanTime = 0f;
            RescanOverlaps();
        }

        private void OnDisable()
        {
            CurrentTarget = null;
            detectedHitboxes.Clear();
            hitboxByCollider.Clear();
            pruneBuffer.Clear();
        }

        private void Update()
        {
            // even if OnTriggerEnter was missed (zone Awake race / no Rigidbody).
            float rescanDelay = detectedHitboxes.Count == 0
                ? Mathf.Min(0.1f, overlapRescanInterval)
                : Mathf.Max(0.05f, overlapRescanInterval);

            if (Time.time >= nextOverlapRescanTime)
            {
                RescanOverlaps();
            }

            if (detectedHitboxes.Count == 0)
            {
                CurrentTarget = null;
                return;
            }

            if (Time.time < nextRetargetTime)
            {
                if (CurrentTarget == null || !isCurrentTargetValid())
                {
                    RefreshCurrentTarget();
                }

                return;
            }

            nextRetargetTime = Time.time + retargetInterval;
            RefreshCurrentTarget();
        }

        public void NotifyEnter(Collider other)
        {
            tryAddCollider(other);
        }

        public void NotifyExit(Collider other)
        {
            if (other == null)
            {
                return;
            }

            if (hitboxByCollider.TryGetValue(other, out DamageableHitbox hitbox))
            {
                hitboxByCollider.Remove(other);
                detectedHitboxes.Remove(hitbox);
                RefreshCurrentTarget();
                return;
            }

            if (other.TryGetComponent(out hitbox))
            {
                detectedHitboxes.Remove(hitbox);
                RefreshCurrentTarget();
            }
        }

        public void RescanOverlaps()
        {
            if (detectZone == null)
            {
                return;
            }

            float worldRadius = detectZone.WorldRadius;
            Vector3 center = detectZone.transform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                worldRadius,
                overlapBuffer,
                enemyMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                tryAddCollider(overlapBuffer[i]);
            }

            RefreshCurrentTarget();
        }

        public void RefreshCurrentTarget()
        {
            pruneDead();

            Transform scanOrigin = origin != null ? origin : transform;
            float bestSqrDistance = float.MaxValue;
            Transform bestTarget = null;

            foreach (DamageableHitbox hitbox in detectedHitboxes)
            {
                if (hitbox == null || hitbox.Damageable == null || !hitbox.Damageable.IsAlive)
                {
                    continue;
                }

                Transform targetPoint = hitbox.AimPoint;
                float sqrDistance = (targetPoint.position - scanOrigin.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestTarget = targetPoint;
                }
            }

            CurrentTarget = bestTarget;
        }

        private void tryAddCollider(Collider other)
        {
            if (!isEnemyLayer(other))
            {
                return;
            }

            DamageableHitbox hitbox = ResolveHitbox(other);
            if (hitbox == null || hitbox.Damageable == null || !hitbox.Damageable.IsAlive)
            {
                return;
            }

            if (detectedHitboxes.Add(hitbox))
            {
                RefreshCurrentTarget();
            }
        }

        private void bindDetectZone()
        {
            if (detectZone == null)
            {
                detectZone = GetComponentInChildren<EnemyDetectZone>(true);
            }

            if (detectZone == null)
            {
                return;
            }

            detectZone.BindScanner(this);
        }

        private void pruneDead()
        {
            pruneBuffer.Clear();
            foreach (DamageableHitbox hitbox in detectedHitboxes)
            {
                if (hitbox == null || hitbox.Damageable == null || !hitbox.Damageable.IsAlive)
                {
                    pruneBuffer.Add(hitbox);
                }
            }

            for (int i = 0; i < pruneBuffer.Count; i++)
            {
                detectedHitboxes.Remove(pruneBuffer[i]);
            }
        }

        private bool isCurrentTargetValid()
        {
            if (CurrentTarget == null)
            {
                return false;
            }

            foreach (DamageableHitbox hitbox in detectedHitboxes)
            {
                if (hitbox != null && hitbox.AimPoint == CurrentTarget && hitbox.Damageable != null && hitbox.Damageable.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private bool isEnemyLayer(Collider other)
        {
            return other != null && enemyMask.Contains(other.gameObject.layer);
        }

        private DamageableHitbox ResolveHitbox(Collider hit)
        {
            if (hit == null)
            {
                return null;
            }

            if (hitboxResolver != null && hitboxResolver.TryResolve(hit, out DamageableHitbox resolvedHitbox))
            {
                hitboxByCollider[hit] = resolvedHitbox;
                return resolvedHitbox;
            }

            if (hitboxByCollider.TryGetValue(hit, out DamageableHitbox cachedHitbox))
            {
                return cachedHitbox;
            }

            hit.TryGetComponent(out DamageableHitbox hitbox);
            hitboxByCollider[hit] = hitbox;
            return hitbox;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (detectZone == null)
            {
                detectZone = GetComponentInChildren<EnemyDetectZone>(true);
            }

            detectZone?.BindScanner(this);
        }
#endif
    }
}
