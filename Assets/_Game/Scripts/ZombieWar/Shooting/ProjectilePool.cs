using UnityEngine;

namespace ZombieWar.Shooting
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private SimpleProjectile[] projectiles;

        private int nextIndex;

        public bool TryFire(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            if (projectiles == null || projectiles.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < projectiles.Length; i++)
            {
                int index = (nextIndex + i) % projectiles.Length;
                SimpleProjectile projectile = projectiles[index];
                if (projectile == null || !projectile.IsAvailable)
                {
                    continue;
                }

                nextIndex = (index + 1) % projectiles.Length;
                projectile.Fire(position, rotation, velocity);
                return true;
            }

            return false;
        }

        public void ReleaseAll()
        {
            if (projectiles == null)
            {
                return;
            }

            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != null)
                {
                    projectiles[i].Release();
                }
            }
        }
    }
}
