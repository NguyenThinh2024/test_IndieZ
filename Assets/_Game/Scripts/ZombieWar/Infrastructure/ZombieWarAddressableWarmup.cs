using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Infrastructure
{
    /// <summary>
    /// Warm Addressables cache before Gameplay. Does not instantiate — download deps
    /// so packs are local and gameplay Instantiates/Get from pool stay fast.
    /// </summary>
    public static class ZombieWarAddressableWarmup
    {
        private static readonly string[] WarmupAddresses =
        {
            "ZombieWar/Levels/Level1",
            "ZombieWar/Levels/Level2",
            "ZombieWar/Enemy/Zombie",
            "ZombieWar/Enemy/ShirtlessBoss",
            "ZombieWar/Enemy/Configs/Zombie",
            "ZombieWar/Enemy/Configs/ZombieRunner",
            "ZombieWar/Enemy/Configs/ZombieShirtlessBoss",
            "ZombieWar/Enemy/Skins/Base",
            "ZombieWar/Enemy/Skins/MaskMap",
            "ZombieWar/Audio/Zombie/Moan001",
            "ZombieWar/Audio/Zombie/Hiss010",
            "ZombieWar/Audio/Zombie/Death004",
            "ZombieWar/Audio/Zombie/Grunt006",
            "ZombieWar/Player/Configs/Soldier",
            "ZombieWar/Player/PlayerArmature",
            "ZombieWar/Player/Soldier",
            "ZombieWar/Weapons/Configs/FAMAS",
            "ZombieWar/Weapons/Configs/AUG",
            "ZombieWar/Weapons/FAMAS",
            "ZombieWar/Weapons/AUG",
            "ZombieWar/Audio/AutoGun_1p_01",
            "ZombieWar/Audio/AutoGun_1p_02",
            "ZombieWar/Vfx/MuzzleFlash",
            "ZombieWar/Vfx/BulletTracer",
        };

        public static IReadOnlyList<string> Addresses => WarmupAddresses;

        public static async UniTask WarmupAsync(
            IProgress<float> progress,
            CancellationToken cancellationToken)
        {
            AsyncOperationHandle initHandle = Addressables.InitializeAsync();
            await initHandle.ToUniTask(cancellationToken: cancellationToken);

            int totalCount = WarmupAddresses.Length;
            progress?.Report(0f);

            for (int i = 0; i < totalCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string address = WarmupAddresses[i];

                try
                {
                    await downloadDependencyAsync(address, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                }

                progress?.Report((i + 1) / (float)totalCount);
            }

            progress?.Report(1f);
        }

        private static async UniTask downloadDependencyAsync(string address, CancellationToken cancellationToken)
        {
            AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(address);
            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}
