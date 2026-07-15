using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ZombieWar.Player
{
    public interface IPlayerWeaponAttach
    {
        void Bind(Animator humanoidAnimator);
    }

    public sealed class PlayerCharacterLoader : MonoBehaviour
    {
        [SerializeField] private AssetReferenceT<TextAsset> characterConfigReference;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerAnimation playerAnimation;
        [SerializeField] private MonoBehaviour playerWeaponAttachBehaviour;

        private PlayerCharacterConfigLoader configLoader;
        private PlayerCharacterPrefabLoader prefabLoader;
        private PlayerAnimatorControllerLoader animatorControllerLoader;
        private PlayerCharacterRuntimeBinder runtimeBinder;
        private PlayerCharacterConfig characterConfig;
        private Animator characterAnimator;
        private IPlayerWeaponAttach playerWeaponAttach;
        private bool hasValidDependencies;

        private void Awake()
        {
            bindLocalDependencies();
            hasValidDependencies = validateDependencies();
            if (!hasValidDependencies)
            {
                return;
            }

            configLoader = new PlayerCharacterConfigLoader(characterConfigReference, this);
            prefabLoader = new PlayerCharacterPrefabLoader(this);
            animatorControllerLoader = new PlayerAnimatorControllerLoader(this);
            runtimeBinder = new PlayerCharacterRuntimeBinder(movement, playerAnimation);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bindLocalDependencies();
        }
#endif

        private void bindLocalDependencies()
        {
            if (movement == null)
            {
                movement = GetComponent<PlayerMovement>();
            }

            if (playerAnimation == null)
            {
                playerAnimation = GetComponent<PlayerAnimation>();
            }

            if (playerWeaponAttachBehaviour == null)
            {
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IPlayerWeaponAttach)
                    {
                        playerWeaponAttachBehaviour = behaviours[i];
                        break;
                    }
                }
            }

            playerWeaponAttach = playerWeaponAttachBehaviour as IPlayerWeaponAttach;

            if (visualRoot == null)
            {
                visualRoot = transform;
            }
        }

        private void OnEnable()
        {
            if (!hasValidDependencies)
            {
                return;
            }

            loadCharacter();
        }

        private void OnDisable()
        {
            configLoader?.Release();
            prefabLoader?.Release();
            animatorControllerLoader?.Release();
        }

        private bool validateDependencies()
        {
            if (characterConfigReference == null || !characterConfigReference.RuntimeKeyIsValid())
            {
                return false;
            }

            if (movement == null)
            {
                return false;
            }

            if (playerAnimation == null)
            {
                return false;
            }

            return true;
        }

        private void loadCharacter()
        {
            configLoader.Load(onCharacterConfigLoaded);
        }

        private void onCharacterConfigLoaded(PlayerCharacterConfig config)
        {
            characterConfig = config;
            runtimeBinder.ApplyStats(characterConfig.Stats);
            prefabLoader.Load(characterConfig.Character, visualRoot, onCharacterPrefabLoaded);
        }

        private void onCharacterPrefabLoaded(Animator animator)
        {
            characterAnimator = animator;
            animatorControllerLoader.Load(characterConfig.Animation, onAnimatorControllerLoaded);
        }

        private void onAnimatorControllerLoaded(RuntimeAnimatorController controller)
        {
            runtimeBinder.BindAnimator(characterConfig.Animation, characterAnimator, controller);
            playerWeaponAttach?.Bind(characterAnimator);
        }
    }
}
