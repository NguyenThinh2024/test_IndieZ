using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Enemy
{
    /// <summary>
    /// Loads zombie JSON TextAsset via Addressables.
    /// Safe to call LoadAsync repeatedly — reuses AssetReference handle / cached config.
    /// </summary>
    public sealed class ZombieEnemyConfigLoader
    {
        private readonly AssetReferenceT<TextAsset> configReference;
        private readonly MonoBehaviour owner;

        private AsyncOperationHandle<TextAsset> configHandle;
        private ZombieEnemyConfig cachedConfig;
        private UniTask<ZombieEnemyConfig> loadTask;
        private bool isLoading;
        private bool ownsHandle;

        public ZombieEnemyConfigLoader(AssetReferenceT<TextAsset> configReference, MonoBehaviour owner)
        {
            this.configReference = configReference;
            this.owner = owner;
        }

        public void Load(Action<ZombieEnemyConfig> completedHandler)
        {
            LoadAsync(owner.GetCancellationTokenOnDestroy())
                .ContinueWith(config => completedHandler?.Invoke(config))
                .Forget();
        }

        public UniTask<ZombieEnemyConfig> LoadAsync(CancellationToken cancellationToken)
        {
            if (cachedConfig != null)
            {
                return UniTask.FromResult(cachedConfig);
            }

            if (isLoading)
            {
                return loadTask;
            }

            isLoading = true;
            loadTask = loadConfigAsync(cancellationToken);
            return loadTask;
        }

        public void Release()
        {
            if (ownsHandle && configHandle.IsValid())
            {
                Addressables.Release(configHandle);
            }
            else if (!ownsHandle
                     && configReference != null
                     && configReference.OperationHandle.IsValid())
            {
                // Loaded through AssetReference.LoadAssetAsync — release via reference API.
                configReference.ReleaseAsset();
            }

            configHandle = default;
            ownsHandle = false;
            cachedConfig = null;
            loadTask = default;
            isLoading = false;
        }

        private async UniTask<ZombieEnemyConfig> loadConfigAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cachedConfig != null)
                {
                    return cachedConfig;
                }

                if (configReference == null || !configReference.RuntimeKeyIsValid())
                {
                    return null;
                }

                TextAsset textAsset = await loadTextAssetAsync(cancellationToken);
                if (textAsset == null)
                {
                    return null;
                }

                ZombieEnemyConfig config = JsonUtility.FromJson<ZombieEnemyConfig>(textAsset.text);
                if (config == null)
                {
                    return null;
                }

                cachedConfig = config;
                return config;
            }
            finally
            {
                isLoading = false;
            }
        }

        private async UniTask<TextAsset> loadTextAssetAsync(CancellationToken cancellationToken)
        {
            // Already loaded into AssetReference.Asset — reuse without a second LoadAssetAsync.
            if (configReference.Asset != null)
            {
                return configReference.Asset as TextAsset;
            }

            if (configReference.OperationHandle.IsValid())
            {
                if (configReference.OperationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    return configReference.OperationHandle.Result as TextAsset;
                }

                configHandle = configReference.OperationHandle.Convert<TextAsset>();
                ownsHandle = false;
                return await configHandle.ToUniTask(cancellationToken: cancellationToken);
            }

            // Prefer RuntimeKey load: Addressables allows multiple handles with ref-count.
            // Avoids "Attempting to load AssetReference that has already been loaded".
            object runtimeKey = configReference.RuntimeKey;
            configHandle = Addressables.LoadAssetAsync<TextAsset>(runtimeKey);
            ownsHandle = true;
            return await configHandle.ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
