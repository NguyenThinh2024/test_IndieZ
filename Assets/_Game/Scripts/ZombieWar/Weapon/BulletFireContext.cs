using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Launch payload for one bullet instance.
    /// </summary>
    public readonly struct BulletFireContext
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly float Speed;
        public readonly float Damage;
        public readonly float Range;
        public readonly LayerMask HitMask;
        public readonly GameObject Owner;
        public readonly GameObject EnvironmentHitVfx;
        public readonly DamageableHitboxResolver HitboxResolver;

        public BulletFireContext(
            Vector3 origin,
            Vector3 direction,
            float speed,
            float damage,
            float range,
            LayerMask hitMask,
            GameObject owner,
            GameObject environmentHitVfx,
            DamageableHitboxResolver hitboxResolver)
        {
            Origin = origin;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Speed = Mathf.Max(1f, speed);
            Damage = Mathf.Max(0f, damage);
            Range = Mathf.Max(0.5f, range);
            HitMask = hitMask;
            Owner = owner;
            EnvironmentHitVfx = environmentHitVfx;
            HitboxResolver = hitboxResolver;
        }
    }
}
