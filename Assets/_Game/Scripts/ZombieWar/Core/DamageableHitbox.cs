using UnityEngine;

namespace ZombieWar.Core
{
    public sealed class DamageableHitbox : MonoBehaviour
    {
        [SerializeField] private Component damageableComponent;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private Collider[] linkedColliders;

        public IDamageable Damageable { get; private set; }
        public Transform AimPoint => aimPoint != null ? aimPoint : transform;
        public Collider[] LinkedColliders => linkedColliders;

        private void Awake()
        {
            Damageable = damageableComponent as IDamageable;
        }
    }
}
