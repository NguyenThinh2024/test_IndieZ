using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ThinhPooling;

namespace ZombieWar.Enemy
{
    public sealed class ZombieEnemyPool
    {
        private readonly AddressableComponentPool<Enemy> pool;

        public ZombieEnemyPool(MonoBehaviour owner)
        {
            pool = new AddressableComponentPool<Enemy>(owner, "ZombieEnemyPool");
        }

        public void Prewarm(ZombieEnemyConfig config, int count, Action completedHandler = null)
        {
            if (!tryGetPrefabAddress(config, out string prefabAddress))
            {
                completedHandler?.Invoke();
                return;
            }

            pool.Prewarm(prefabAddress, count, completedHandler);
        }

        public UniTask PrewarmAsync(ZombieEnemyConfig config, int count, CancellationToken cancellationToken)
        {
            if (!tryGetPrefabAddress(config, out string prefabAddress))
            {
                return UniTask.CompletedTask;
            }

            return pool.PrewarmAsync(prefabAddress, count, cancellationToken);
        }

        public void Get(
            ZombieEnemyConfig config,
            Vector3 position,
            Quaternion rotation,
            Action<Enemy, ZombieEnemyConfig> completedHandler)
        {
            if (!tryGetPrefabAddress(config, out string prefabAddress))
            {
                return;
            }

            pool.Get(prefabAddress, position, rotation, zombie => completedHandler?.Invoke(zombie, config));
        }

        public UniTask<Enemy> GetAsync(
            ZombieEnemyConfig config,
            Vector3 position,
            Quaternion rotation,
            CancellationToken cancellationToken)
        {
            if (!tryGetPrefabAddress(config, out string prefabAddress))
            {
                return UniTask.FromResult<Enemy>(null);
            }

            return pool.GetAsync(prefabAddress, position, rotation, cancellationToken);
        }

        public void ReleaseAll()
        {
            pool.ReleaseAll();
        }

        private static bool tryGetPrefabAddress(ZombieEnemyConfig config, out string prefabAddress)
        {
            prefabAddress = config != null && config.Character != null ? config.Character.PrefabAddress : null;
            return !string.IsNullOrWhiteSpace(prefabAddress);
        }
    }
}
