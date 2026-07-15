using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Level
{
    /// <summary>
    /// Owns one Addressable map instance + optional preload handle for the next map.
    /// Instantiates only the active map. Preload warms the next asset so swap stays cheap.
    /// </summary>
    public sealed class LevelMapLoader
    {
        private readonly MonoBehaviour owner;

        private AsyncOperationHandle<GameObject> instanceHandle;
        private AsyncOperationHandle<GameObject> preloadHandle;
        private GameObject mapInstance;
        private string loadedAddress;
        private string preloadedAddress;
        private bool isLoading;

        public LevelMapLoader(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        public GameObject MapInstance => mapInstance;
        public string LoadedAddress => loadedAddress;
        public bool IsLoading => isLoading;
        public bool HasMap => mapInstance != null;

        public async UniTask<GameObject> LoadAsync(
            string address,
            Transform parent,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            if (HasMap && string.Equals(loadedAddress, address, StringComparison.Ordinal))
            {
                return mapInstance;
            }

            if (isLoading)
            {
                return null;
            }

            isLoading = true;
            try
            {
                ReleaseInstanceOnly();

                instanceHandle = Addressables.InstantiateAsync(address, parent);
                GameObject instance = await instanceHandle.ToUniTask(cancellationToken: cancellationToken);

                if (instance == null)
                {
                    ReleaseInstanceOnly();
                    return null;
                }

                mapInstance = instance;
                loadedAddress = address;
                resetLocalTransform(instance.transform);

                // Preload for this address is no longer needed once it is the active map.
                if (string.Equals(preloadedAddress, address, StringComparison.Ordinal))
                {
                    ReleasePreloadOnly();
                }

                return mapInstance;
            }
            catch (OperationCanceledException)
            {
                ReleaseInstanceOnly();
                throw;
            }
            catch (Exception exception)
            {
                ReleaseInstanceOnly();
                return null;
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// Warm Addressables cache for the next map without instantiating it.
        /// </summary>
        public async UniTask PreloadAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            if (string.Equals(preloadedAddress, address, StringComparison.Ordinal) && preloadHandle.IsValid())
            {
                return;
            }

            if (string.Equals(loadedAddress, address, StringComparison.Ordinal))
            {
                return;
            }

            ReleasePreloadOnly();

            try
            {
                preloadHandle = Addressables.LoadAssetAsync<GameObject>(address);
                await preloadHandle.ToUniTask(cancellationToken: cancellationToken);
                if (preloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    preloadedAddress = address;
                    return;
                }

                ReleasePreloadOnly();
            }
            catch (OperationCanceledException)
            {
                ReleasePreloadOnly();
                throw;
            }
            catch (Exception exception)
            {
                ReleasePreloadOnly();
            }
        }

        public void Release()
        {
            ReleaseInstanceOnly();
            ReleasePreloadOnly();
        }

        private void ReleaseInstanceOnly()
        {
            if (instanceHandle.IsValid())
            {
                Addressables.ReleaseInstance(instanceHandle);
            }

            instanceHandle = default;
            mapInstance = null;
            loadedAddress = null;
        }

        private void ReleasePreloadOnly()
        {
            if (preloadHandle.IsValid())
            {
                Addressables.Release(preloadHandle);
            }

            preloadHandle = default;
            preloadedAddress = null;
        }

        private static void resetLocalTransform(Transform mapTransform)
        {
            mapTransform.localPosition = Vector3.zero;
            mapTransform.localRotation = Quaternion.identity;
            mapTransform.localScale = Vector3.one;
        }
    }
}