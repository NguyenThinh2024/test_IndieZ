using System.Collections.Generic;
using UnityEngine;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Central tick owner for active <see cref="BulletProjectile"/> instances.
    /// </summary>
    public sealed class BulletProjectileSystem : MonoBehaviour
    {
        private readonly List<BulletProjectile> activeBullets = new List<BulletProjectile>(64);

        public void Register(BulletProjectile bullet)
        {
            if (bullet == null)
            {
                return;
            }

            activeBullets.Add(bullet);
        }

        public void Unregister(BulletProjectile bullet)
        {
            if (bullet == null)
            {
                return;
            }

            activeBullets.Remove(bullet);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                BulletProjectile bullet = activeBullets[i];
                if (bullet == null)
                {
                    activeBullets.RemoveAt(i);
                    continue;
                }

                if (!bullet.Tick(deltaTime))
                {
                    // Bullet recycled itself and should already Unregister; remove if stale.
                    if (i < activeBullets.Count && activeBullets[i] == bullet)
                    {
                        activeBullets.RemoveAt(i);
                    }
                }
            }
        }

        private void OnDisable()
        {
            activeBullets.Clear();
        }
    }
}
