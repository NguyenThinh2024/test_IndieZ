using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Player
{
    public sealed class PlayerAnimatorControllerLoader
    {
        private readonly MonoBehaviour owner;

        private AsyncOperationHandle<RuntimeAnimatorController> animatorControllerHandle;
        private Action<RuntimeAnimatorController> completedHandler;

        public PlayerAnimatorControllerLoader(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        public void Load(PlayerCharacterAnimationConfig config, Action<RuntimeAnimatorController> completedHandler)
        {
            this.completedHandler = completedHandler;
            if (config == null || string.IsNullOrWhiteSpace(config.ControllerAddress))
            {
                completedHandler?.Invoke(null);
                return;
            }

            animatorControllerHandle = Addressables.LoadAssetAsync<RuntimeAnimatorController>(config.ControllerAddress);
            animatorControllerHandle.Completed += loadAnimatorControllerHandler;
        }

        public void Release()
        {
            if (!animatorControllerHandle.IsValid())
            {
                return;
            }

            animatorControllerHandle.Completed -= loadAnimatorControllerHandler;
            Addressables.Release(animatorControllerHandle);
            animatorControllerHandle = default;
            completedHandler = null;
        }

        private void loadAnimatorControllerHandler(AsyncOperationHandle<RuntimeAnimatorController> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                completedHandler?.Invoke(null);
                return;
            }

            completedHandler?.Invoke(handle.Result);
        }
    }
}
