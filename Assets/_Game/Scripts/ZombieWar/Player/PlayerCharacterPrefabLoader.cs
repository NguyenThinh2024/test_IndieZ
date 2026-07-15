using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZombieWar.Player
{
    public sealed class PlayerCharacterPrefabLoader
    {
        private readonly MonoBehaviour owner;
        private readonly InstanceSetup instanceSetup = new InstanceSetup();

        private AsyncOperationHandle<GameObject> characterInstanceHandle;
        private Action<Animator> completedHandler;
        private GameObject characterInstance;
        private bool isLoading;

        public PlayerCharacterPrefabLoader(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        public void Load(PlayerCharacterAssetConfig config, Transform parent, Action<Animator> completedHandler)
        {
            if (isLoading || characterInstance != null)
            {
                return;
            }

            if (config == null || string.IsNullOrWhiteSpace(config.PrefabAddress))
            {
                return;
            }

            this.completedHandler = completedHandler;
            isLoading = true;

            try
            {
                characterInstanceHandle = Addressables.InstantiateAsync(config.PrefabAddress, parent);
                characterInstanceHandle.Completed += onInstantiateCompleted;
            }
            catch (InvalidKeyException exception)
            {
                isLoading = false;
            }
        }

        public void Release()
        {
            if (!characterInstanceHandle.IsValid())
            {
                return;
            }

            characterInstanceHandle.Completed -= onInstantiateCompleted;
            Addressables.ReleaseInstance(characterInstanceHandle);
            characterInstanceHandle = default;
            characterInstance = null;
            completedHandler = null;
            isLoading = false;
        }

        private void onInstantiateCompleted(AsyncOperationHandle<GameObject> handle)
        {
            isLoading = false;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                return;
            }

            characterInstance = handle.Result;
            Animator characterAnimator = instanceSetup.Prepare(characterInstance);
            completedHandler?.Invoke(characterAnimator);
        }

        // One-time prepare for the spawned character instance (owned by PrefabLoader).
        private sealed class InstanceSetup
        {
            private static readonly string[] FirstPersonRootNames =
            {
                "FPS_HANDS",
            };

            public Animator Prepare(GameObject characterInstance)
            {
                if (characterInstance == null)
                {
                    return null;
                }

                resetLocalTransform(characterInstance.transform);
                disableFirstPersonRoots(characterInstance.transform);
                return selectPrimaryAnimator(characterInstance);
            }

            private static void resetLocalTransform(Transform characterTransform)
            {
                characterTransform.localPosition = Vector3.zero;
                characterTransform.localRotation = Quaternion.identity;
                characterTransform.localScale = new Vector3(2f, 2f, 2f);
            }

            private static void disableFirstPersonRoots(Transform characterRoot)
            {
                for (int i = 0; i < FirstPersonRootNames.Length; i++)
                {
                    Transform firstPersonRoot = findChildByName(characterRoot, FirstPersonRootNames[i]);
                    if (firstPersonRoot != null)
                    {
                        firstPersonRoot.gameObject.SetActive(false);
                    }
                }
            }

            private static Transform findChildByName(Transform root, string objectName)
            {
                if (root.name == objectName)
                {
                    return root;
                }

                for (int i = 0; i < root.childCount; i++)
                {
                    Transform found = findChildByName(root.GetChild(i), objectName);
                    if (found != null)
                    {
                        return found;
                    }
                }

                return null;
            }

            private static Animator selectPrimaryAnimator(GameObject characterRoot)
            {
                Animator[] animators = characterRoot.GetComponentsInChildren<Animator>(true);
                if (animators.Length == 0)
                {
                    return null;
                }

                Animator primary = null;
                RuntimeAnimatorController sharedController = null;

                for (int i = 0; i < animators.Length; i++)
                {
                    Animator candidate = animators[i];
                    if (candidate.runtimeAnimatorController != null)
                    {
                        sharedController = candidate.runtimeAnimatorController;
                    }

                    if (!isValidHumanoidAnimator(candidate))
                    {
                        continue;
                    }

                    if (primary == null || isDeeper(candidate.transform, primary.transform))
                    {
                        primary = candidate;
                    }
                }

                if (primary == null)
                {
                    primary = animators[0];
                }

                if (primary.runtimeAnimatorController == null)
                {
                    primary.runtimeAnimatorController = sharedController;
                }

                for (int i = 0; i < animators.Length; i++)
                {
                    Animator other = animators[i];
                    if (other == primary)
                    {
                        continue;
                    }

                    other.enabled = false;
                    other.runtimeAnimatorController = null;
                }

                primary.enabled = true;
                primary.applyRootMotion = false;
                primary.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                return primary;
            }

            private static bool isValidHumanoidAnimator(Animator animator)
            {
                return animator.avatar != null
                       && animator.avatar.isValid
                       && animator.avatar.isHuman
                       && animator.GetBoneTransform(HumanBodyBones.Hips) != null;
            }

            private static bool isDeeper(Transform candidate, Transform current)
            {
                return getDepth(candidate) > getDepth(current);
            }

            private static int getDepth(Transform transform)
            {
                int depth = 0;
                Transform current = transform;
                while (current.parent != null)
                {
                    depth++;
                    current = current.parent;
                }

                return depth;
            }
        }
    }
}
