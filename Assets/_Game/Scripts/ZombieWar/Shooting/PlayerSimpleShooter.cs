using UnityEngine;

namespace ZombieWar.Shooting
{
    public sealed class PlayerSimpleShooter : MonoBehaviour
    {
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float cooldown = 0.2f;

        private float nextShootTime;

        public void Shoot()
        {
            if (projectilePool == null || firePoint == null || Time.time < nextShootTime)
            {
                return;
            }

            nextShootTime = Time.time + Mathf.Max(0.01f, cooldown);
            Vector3 velocity = firePoint.forward * projectileSpeed;
            projectilePool.TryFire(firePoint.position, firePoint.rotation, velocity);
        }
    }
}
