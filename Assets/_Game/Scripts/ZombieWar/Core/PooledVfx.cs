using System.Collections.Generic;
using TBN;
using UnityEngine;

namespace ZombieWar.Core
{
    public static class PooledVfx
    {
        private static readonly List<ParticleSystem> ParticleBuffer = new List<ParticleSystem>(64);

        public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifeTime = 2f, Transform parent = null)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = prefab.Spawn(position, rotation, parent);
            if (instance == null)
            {
                return;
            }

            RestartParticles(instance);

            if (lifeTime > 0f)
            {
                instance.Recycle(lifeTime);
            }
        }

        /// <summary>
        /// Prefabs authored with playOnAwake=false (e.g. PlayerUpgradeFX) need an explicit Play after spawn/pool reuse.
        /// </summary>
        public static void RestartParticles(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            ParticleBuffer.Clear();
            root.GetComponentsInChildren(true, ParticleBuffer);

            for (int i = 0; i < ParticleBuffer.Count; i++)
            {
                ParticleSystem particle = ParticleBuffer[i];
                if (particle == null)
                {
                    continue;
                }

                particle.Clear(true);
                particle.Play(true);
            }
        }
    }
}
