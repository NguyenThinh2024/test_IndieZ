using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Player
{
    public sealed class PlayerCharacterConfigLoader
    {
        private readonly AssetReferenceT<TextAsset> configReference;
        private readonly MonoBehaviour owner;

        private AsyncOperationHandle<TextAsset> configHandle;
        private Action<PlayerCharacterConfig> completedHandler;

        public PlayerCharacterConfigLoader(AssetReferenceT<TextAsset> configReference, MonoBehaviour owner)
        {
            this.configReference = configReference;
            this.owner = owner;
        }

        public void Load(Action<PlayerCharacterConfig> completedHandler)
        {
            if (configHandle.IsValid())
            {
                return;
            }

            if (configReference == null || !configReference.RuntimeKeyIsValid())
            {
                return;
            }

            this.completedHandler = completedHandler;
            configHandle = configReference.LoadAssetAsync<TextAsset>();
            configHandle.Completed += loadConfigHandler;
        }

        public void Release()
        {
            if (!configHandle.IsValid())
            {
                return;
            }

            configHandle.Completed -= loadConfigHandler;
            configReference.ReleaseAsset();
            configHandle = default;
            completedHandler = null;
        }

        private void loadConfigHandler(AsyncOperationHandle<TextAsset> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                return;
            }

            PlayerCharacterConfig config = JsonUtility.FromJson<PlayerCharacterConfig>(handle.Result.text);
            if (config == null)
            {
                return;
            }

            completedHandler?.Invoke(config);
        }
    }
}
