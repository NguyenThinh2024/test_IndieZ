#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    public static class PlayerAnimatorSetup
    {
        private const string OutputFolder = "Assets/_Game/Art/Animations/Player";
        private const string ControllerPath = OutputFolder + "/PlayerCombat.controller";
        private const string MaskPath = OutputFolder + "/UpperBody.mask";
        private const string ControllerAddress = "ZombieWar/Player/Anim/PlayerCombat";

        private const string IdlePath =
            "Assets/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx";
        private const string WalkPath =
            "Assets/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Walk_N.anim.fbx";
        private const string RunPath =
            "Assets/Survivalist/Basemesh/Shoot Rifle.fbx";

        [MenuItem("Zombie War/Animation/Setup Player Combat Animator")]
        public static void SetupPlayerCombatAnimator()
        {
            ensureFolder("Assets/_Game/Art");
            ensureFolder("Assets/_Game/Art/Animations");
            ensureFolder(OutputFolder);

            AvatarMask upperBodyMask = createOrLoadUpperBodyMask();
            AnimatorController controller = createOrLoadController();
            buildController(controller, upperBodyMask);
            registerAddressable(ControllerPath, ControllerAddress);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(upperBodyMask);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = controller;
            Debug.Log(
                $"[Zombie War] Player animator ready.\n" +
                $"- Controller: {ControllerPath}\n" +
                $"- Addressable: {ControllerAddress}\n" +
                $"- Set SoldierCharacterConfig.json animation.controllerAddress = \"{ControllerAddress}\"\n" +
                $"- Tip: assign a real Shoot clip on UpperBody/Shoot state when you have one.");
        }

        private static void ensureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static AvatarMask createOrLoadUpperBodyMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
            }

            mask.name = "UpperBody";
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
                bool include =
                    part == AvatarMaskBodyPart.Body
                    || part == AvatarMaskBodyPart.Head
                    || part == AvatarMaskBodyPart.LeftArm
                    || part == AvatarMaskBodyPart.RightArm
                    || part == AvatarMaskBodyPart.LeftFingers
                    || part == AvatarMaskBodyPart.RightFingers;

                mask.SetHumanoidBodyPartActive(part, include);
            }

            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController createOrLoadController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null)
            {
                return controller;
            }

            return AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        private static void buildController(AnimatorController controller, AvatarMask upperBodyMask)
        {
            ensureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            ensureParameter(controller, "Shoot", AnimatorControllerParameterType.Trigger);
            ensureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            ensureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);

            while (controller.layers.Length > 1)
            {
                controller.RemoveLayer(controller.layers.Length - 1);
            }

            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = "Base Layer";
            baseLayer.defaultWeight = 1f;
            controller.layers = new[] { baseLayer };

            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
            clearStateMachine(baseMachine);

            Motion idle = loadClip(IdlePath, "Stand--Idle.anim");
            Motion walk = loadClip(WalkPath, "Locomotion--Walk_N.anim");
            Motion run = loadClip(RunPath, "Shoot_Rifle");

            BlendTree locomotion = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(locomotion, controller);

            if (idle != null)
            {
                locomotion.AddChild(idle, 0f);
            }

            if (walk != null)
            {
                locomotion.AddChild(walk, 0.5f);
            }

            if (run != null)
            {
                locomotion.AddChild(run, 1f);
            }

            AnimatorState locomotionState = baseMachine.AddState("Locomotion", new Vector3(300f, 0f, 0f));
            locomotionState.motion = locomotion;
            baseMachine.defaultState = locomotionState;

            AnimatorControllerLayer upperBodyLayer = new AnimatorControllerLayer
            {
                name = "UpperBody",
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                avatarMask = upperBodyMask,
                stateMachine = new AnimatorStateMachine { name = "UpperBody", hideFlags = HideFlags.HideInHierarchy }
            };

            AssetDatabase.AddObjectToAsset(upperBodyLayer.stateMachine, controller);

            AnimatorState emptyState = upperBodyLayer.stateMachine.AddState("Empty", new Vector3(200f, 0f, 0f));
            upperBodyLayer.stateMachine.defaultState = emptyState;

            AnimatorState shootState = upperBodyLayer.stateMachine.AddState("Shoot", new Vector3(450f, 0f, 0f));
            // Placeholder: reuse Idle until a dedicated fire clip exists.
            shootState.motion = idle;

            AnimatorStateTransition toShoot = emptyState.AddTransition(shootState);
            toShoot.hasExitTime = false;
            toShoot.duration = 0.05f;
            toShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

            AnimatorStateTransition backToEmpty = shootState.AddTransition(emptyState);
            backToEmpty.hasExitTime = true;
            backToEmpty.exitTime = 0.9f;
            backToEmpty.duration = 0.1f;

            controller.AddLayer(upperBodyLayer);
        }

        private static void ensureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, type);
        }

        private static void clearStateMachine(AnimatorStateMachine stateMachine)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = states.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveState(states[i].state);
            }
        }

        private static Motion loadClip(string fbxPath, string clipNameHint)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning($"[Zombie War] Animation asset missing: {fbxPath}");
                return null;
            }

            AnimationClip best = null;
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (clip.name.IndexOf(clipNameHint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clip;
                }

                if (best == null)
                {
                    best = clip;
                }
            }

            return best;
        }

        private static void registerAddressable(string assetPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogWarning("[Zombie War] Addressables settings missing. Mark controller Addressable manually.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetAddress(address);
            EditorUtility.SetDirty(settings);
        }
    }
}
#endif
