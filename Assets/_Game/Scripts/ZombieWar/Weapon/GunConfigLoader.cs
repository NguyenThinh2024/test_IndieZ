using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Weapon
{
    /// <summary>
    /// Loads one gun JSON TextAsset from Addressables into GunData, then optional VFX/audio assets.
    /// </summary>
    public sealed class GunConfigLoader
    {
        private readonly MonoBehaviour owner;
        private AsyncOperationHandle<TextAsset> configHandle;
        private readonly List<AsyncOperationHandle> runtimeAssetHandles = new List<AsyncOperationHandle>(4);
        private Action<GunData> completedHandler;

        public GunConfigLoader(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        public void Load(AssetReferenceT<TextAsset> configReference, Action<GunData> completedHandler)
        {
            this.completedHandler = completedHandler;

            if (configReference == null || !configReference.RuntimeKeyIsValid())
            {
                completedHandler?.Invoke(null);
                return;
            }

            try
            {
                configHandle = configReference.LoadAssetAsync<TextAsset>();
                configHandle.Completed += onConfigLoaded;
            }
            catch (InvalidKeyException exception)
            {
                completedHandler?.Invoke(null);
            }
        }

        public void Load(string configAddress, Action<GunData> completedHandler)
        {
            this.completedHandler = completedHandler;

            if (string.IsNullOrWhiteSpace(configAddress))
            {
                completedHandler?.Invoke(null);
                return;
            }

            try
            {
                configHandle = Addressables.LoadAssetAsync<TextAsset>(configAddress);
                configHandle.Completed += onConfigLoaded;
            }
            catch (InvalidKeyException exception)
            {
                completedHandler?.Invoke(null);
            }
        }

        public void Release()
        {
            if (configHandle.IsValid())
            {
                configHandle.Completed -= onConfigLoaded;
                Addressables.Release(configHandle);
                configHandle = default;
            }

            for (int i = 0; i < runtimeAssetHandles.Count; i++)
            {
                if (runtimeAssetHandles[i].IsValid())
                {
                    Addressables.Release(runtimeAssetHandles[i]);
                }
            }

            runtimeAssetHandles.Clear();
            completedHandler = null;
        }

        private void onConfigLoaded(AsyncOperationHandle<TextAsset> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                completedHandler?.Invoke(null);
                return;
            }

            GunData config = JsonUtility.FromJson<GunData>(handle.Result.text);
            if (config == null)
            {
                completedHandler?.Invoke(null);
                return;
            }

            loadRuntimeAssets(config);
        }

        private void loadRuntimeAssets(GunData config)
        {
            int pending = 0;

            void completeIfDone()
            {
                if (pending > 0)
                {
                    return;
                }

                completedHandler?.Invoke(config);
            }

            void loadGameObject(string address, Action<GameObject> assign)
            {
                if (string.IsNullOrWhiteSpace(address))
                {
                    return;
                }

                pending++;
                try
                {
                    AsyncOperationHandle<GameObject> assetHandle = Addressables.LoadAssetAsync<GameObject>(address);
                    runtimeAssetHandles.Add(assetHandle);
                    assetHandle.Completed += operation =>
                    {
                        if (operation.Status == AsyncOperationStatus.Succeeded)
                        {
                            assign(operation.Result);
                        }
                        pending--;
                        completeIfDone();
                    };
                }
                catch (InvalidKeyException)
                {
                    pending--;
                    completeIfDone();
                }
            }

            void loadAudio(string address, Action<AudioClip> assign)
            {
                if (string.IsNullOrWhiteSpace(address))
                {
                    return;
                }

                pending++;
                try
                {
                    AsyncOperationHandle<AudioClip> assetHandle = Addressables.LoadAssetAsync<AudioClip>(address);
                    runtimeAssetHandles.Add(assetHandle);
                    assetHandle.Completed += operation =>
                    {
                        if (operation.Status == AsyncOperationStatus.Succeeded)
                        {
                            assign(operation.Result);
                        }
                        pending--;
                        completeIfDone();
                    };
                }
                catch (InvalidKeyException)
                {
                    pending--;
                    completeIfDone();
                }
            }

            loadGameObject(config.BulletPrefabAddress, prefab => config.BulletPrefab = prefab);
            loadGameObject(config.MuzzleVfxAddress, prefab => config.MuzzleVfxPrefab = prefab);
            loadGameObject(config.HitVfxAddress, prefab => config.HitVfxPrefab = prefab);
            loadAudio(config.FireClipAddress, clip => config.FireClip = clip);

            completeIfDone();
        }
    }
}
