using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Enemy
{
    /// <summary>
    /// Loads shared Addressable presentation assets (audio, skins) for zombie configs
    /// and applies them before Enemy.Initialize / OnSpawn.
    /// Owned by WaveManager — not a singleton.
    /// </summary>
    public sealed class ZombieEnemyPresentationAssets
    {
        private readonly MonoBehaviour owner;
        private readonly Dictionary<string, AudioClip> audioByAddress = new Dictionary<string, AudioClip>(8);
        private readonly Dictionary<string, Material> skinByAddress = new Dictionary<string, Material>(4);
        private readonly List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>(12);

        public ZombieEnemyPresentationAssets(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        public async UniTask EnsureLoadedAsync(ZombieEnemyConfig config, CancellationToken cancellationToken)
        {
            if (config == null)
            {
                return;
            }

            ZombieEnemyAudioConfig audio = config.Audio;
            if (audio != null)
            {
                audio.ChaseClip = await loadAudioClipAsync(audio.ChaseClipAddress, cancellationToken);
                audio.HitClip = await loadAudioClipAsync(audio.HitClipAddress, cancellationToken);
                audio.DeathClip = await loadAudioClipAsync(audio.DeathClipAddress, cancellationToken);
            }

            string skinAddress = config.Character != null ? config.Character.SkinMaterialAddress : null;
            if (!string.IsNullOrWhiteSpace(skinAddress) && !skinByAddress.ContainsKey(skinAddress))
            {
                Material material = await loadMaterialAsync(skinAddress, cancellationToken);
                if (material != null)
                {
                    skinByAddress[skinAddress] = material;
                }
            }
        }

        public void Apply(Enemy zombie, ZombieEnemyConfig config)
        {
            if (zombie == null || config == null)
            {
                return;
            }

            applyAudio(zombie, config);
            applySkin(zombie, config);
        }

        public void Release()
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i].IsValid())
                {
                    Addressables.Release(handles[i]);
                }
            }

            handles.Clear();
            audioByAddress.Clear();
            skinByAddress.Clear();
        }

        private void applyAudio(Enemy zombie, ZombieEnemyConfig config)
        {
            if (config.Audio == null)
            {
                return;
            }

            ZombieAudio zombieAudio = zombie.Audio;
            if (zombieAudio == null)
            {
                zombieAudio = zombie.GetComponent<ZombieAudio>();
            }

            if (zombieAudio == null)
            {
                zombieAudio = zombie.gameObject.AddComponent<ZombieAudio>();
            }

            zombieAudio.ApplyConfig(config.Audio);
        }

        private void applySkin(Enemy zombie, ZombieEnemyConfig config)
        {
            if (config.Character == null)
            {
                return;
            }

            string address = config.Character.SkinMaterialAddress;
            if (string.IsNullOrWhiteSpace(address) || !skinByAddress.TryGetValue(address, out Material skin))
            {
                return;
            }

            ZombieVisualSkin visualSkin = zombie.GetComponent<ZombieVisualSkin>();
            if (visualSkin == null)
            {
                visualSkin = zombie.GetComponentInChildren<ZombieVisualSkin>(true);
            }

            if (visualSkin == null)
            {
                visualSkin = zombie.gameObject.AddComponent<ZombieVisualSkin>();
            }

            visualSkin.ApplySkin(skin);
        }

        private async UniTask<AudioClip> loadAudioClipAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            if (audioByAddress.TryGetValue(address, out AudioClip cached))
            {
                return cached;
            }

            try
            {
                AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(address);
                handles.Add(handle);
                AudioClip clip = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (clip != null)
                {
                    audioByAddress[address] = clip;
                }

                return clip;
            }
            catch (InvalidKeyException exception)
            {
                return null;
            }
        }

        private async UniTask<Material> loadMaterialAsync(string address, CancellationToken cancellationToken)
        {
            try
            {
                AsyncOperationHandle<Material> handle = Addressables.LoadAssetAsync<Material>(address);
                handles.Add(handle);
                return await handle.ToUniTask(cancellationToken: cancellationToken);
            }
            catch (InvalidKeyException exception)
            {
                return null;
            }
        }
    }
}
