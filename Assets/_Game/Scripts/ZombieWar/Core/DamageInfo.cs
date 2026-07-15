using UnityEngine;

namespace ZombieWar.Core
{
    public readonly struct DamageInfo
    {
        public readonly float Damage;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;
        public readonly GameObject Source;
        public readonly DamageType DamageType;

        public DamageInfo(float damage, Vector3 hitPoint, Vector3 hitDirection, GameObject source, DamageType damageType)
        {
            Damage = damage;
            HitPoint = hitPoint;
            HitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.forward;
            Source = source;
            DamageType = damageType;
        }
    }

    public enum DamageType
    {
        Bullet,
        Explosion,
        Melee
    }
}
