using TBN;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// One pooled bullet. Movement is ticked by <see cref="BulletProjectileSystem"/>.
    /// </summary>
    public sealed class BulletProjectile : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trail;

        private BulletProjectileSystem tickSystem;
        private Vector3 direction;
        private float speed;
        private float damage;
        private float traveled;
        private float maxRange;
        private LayerMask hitMask;
        private GameObject owner;
        private GameObject environmentHitVfx;
        private DamageableHitboxResolver hitboxResolver;
        private bool isActive;

        private void Awake()
        {
            if (trail == null)
            {
                trail = GetComponentInChildren<TrailRenderer>(true);
            }
        }

        public void Launch(in BulletFireContext context, BulletProjectileSystem system)
        {
            // Clear stale registration when reusing a pooled instance.
            tickSystem?.Unregister(this);
            tickSystem = system;
            direction = context.Direction;
            speed = context.Speed;
            damage = context.Damage;
            maxRange = context.Range;
            hitMask = context.HitMask;
            owner = context.Owner;
            environmentHitVfx = context.EnvironmentHitVfx;
            hitboxResolver = context.HitboxResolver;
            traveled = 0f;
            isActive = true;

            transform.SetPositionAndRotation(context.Origin, Quaternion.LookRotation(direction));
            clearTrail();
            tickSystem?.Register(this);
        }

        /// <returns>True while still flying.</returns>
        public bool Tick(float deltaTime)
        {
            if (!isActive)
            {
                return false;
            }

            float step = speed * deltaTime;
            if (step <= 0f)
            {
                return true;
            }

            Vector3 origin = transform.position;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, step + 0.02f, hitMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                applyHit(hit);
                despawn();
                return false;
            }

            transform.position = origin + direction * step;
            traveled += step;
            if (traveled >= maxRange)
            {
                despawn();
                return false;
            }

            return true;
        }

        private void OnDisable()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            tickSystem?.Unregister(this);
            clearTrail();
        }

        private void applyHit(in RaycastHit hit)
        {
            DamageableHitbox hitbox = resolveHitbox(hit.collider);
            if (hitbox != null && hitbox.Damageable != null)
            {
                DamageInfo damageInfo = new DamageInfo(damage, hit.point, direction, owner, DamageType.Bullet);
                hitbox.Damageable.TakeDamage(damageInfo);
                return;
            }

            if (environmentHitVfx != null)
            {
                PooledVfx.Spawn(environmentHitVfx, hit.point, Quaternion.LookRotation(hit.normal), 1.5f);
            }
        }

        private DamageableHitbox resolveHitbox(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            if (hitboxResolver != null && hitboxResolver.TryResolve(hitCollider, out DamageableHitbox resolved))
            {
                return resolved;
            }

            hitCollider.TryGetComponent(out DamageableHitbox hitbox);
            return hitbox;
        }

        private void despawn()
        {
            isActive = false;
            tickSystem?.Unregister(this);
            clearTrail();
            gameObject.Recycle();
        }

        private void clearTrail()
        {
            if (trail != null)
            {
                trail.Clear();
            }
        }
    }
}
