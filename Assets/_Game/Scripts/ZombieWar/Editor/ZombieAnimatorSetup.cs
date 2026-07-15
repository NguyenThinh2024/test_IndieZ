#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Builds Zombie.controller (MoveSpeed blend + Attack/Hit/Die) from FBXZombie clips
    /// and wires it onto Resources/ZombieWar/Zombie/Zombie.prefab.
    /// Menu: Zombie War/Animation/Setup Zombie Animator
    /// </summary>
    public static class ZombieAnimatorSetup
    {
        private const string FbxFolder = "Assets/_Game/FBXZombie";
        private const string MeshFbxPath = FbxFolder + "/Zombie.fbx";
        private const string OutputFolder = "Assets/_Game/Art/Animations/Zombie";
        private const string ControllerPath = OutputFolder + "/Zombie.controller";
        private const string PrefabPath = "Assets/_Game/Resources/ZombieWar/Zombie/Zombie.prefab";
        private const string VisualChildName = "ZombieVisual";

        private const string IdleFbx = FbxFolder + "/zombie idle.fbx";
        private const string WalkFbx = FbxFolder + "/zombie walk.fbx";
        private const string RunFbx = FbxFolder + "/zombie run.fbx";
        private const string AttackFbx = FbxFolder + "/zombie attack.fbx";
        private const string HitFbx = FbxFolder + "/zombie scream.fbx";
        private const string DieFbx = FbxFolder + "/zombie death.fbx";

        [MenuItem("Zombie War/Animation/Setup Zombie Animator")]
        public static void SetupZombieAnimator()
        {
            ensureFolder("Assets/_Game/Art");
            ensureFolder("Assets/_Game/Art/Animations");
            ensureFolder(OutputFolder);

            configureFbxImports();

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = createOrLoadController();
            buildController(controller);
            wirePrefab(controller);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = controller;
            Debug.Log(
                $"[Zombie War] Zombie animator ready.\n" +
                $"- Controller: {ControllerPath}\n" +
                $"- Prefab: {PrefabPath}\n" +
                $"- Params: MoveSpeed, Attack, Hit, Die");
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

        private static void configureFbxImports()
        {
            configureMeshFbx(MeshFbxPath);

            // Mixamo anim FBX roots differ from mesh (file name vs Zombie_Skel).
            // CreateFromThisModel + Humanoid retargets onto ZombieAvatar at runtime.
            configureClipFbx(IdleFbx, "Zombie_Idle", loop: true);
            configureClipFbx(WalkFbx, "Zombie_Walk", loop: true);
            configureClipFbx(RunFbx, "Zombie_Run", loop: true);
            configureClipFbx(AttackFbx, "Zombie_Attack", loop: false);
            configureClipFbx(HitFbx, "Zombie_Hit", loop: false);
            configureClipFbx(DieFbx, "Zombie_Die", loop: false);

            configureClipFbx(FbxFolder + "/zombie crawl.fbx", "Zombie_Crawl", loop: true);
            configureClipFbx(FbxFolder + "/running crawl.fbx", "Zombie_CrawlRun", loop: true);
            configureClipFbx(FbxFolder + "/zombie dying.fbx", "Zombie_Dying", loop: false);
            configureClipFbx(FbxFolder + "/zombie biting.fbx", "Zombie_Bite", loop: false);
            configureClipFbx(FbxFolder + "/zombie biting (2).fbx", "Zombie_Bite2", loop: false);
            configureClipFbx(FbxFolder + "/zombie neck bite.fbx", "Zombie_NeckBite", loop: false);
        }

        private static void configureMeshFbx(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        private static void configureClipFbx(string path, string clipName, bool loop)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Zombie War] Missing FBX: {path}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;

            ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0)
            {
                defaults = importer.clipAnimations;
            }

            if (defaults != null && defaults.Length > 0)
            {
                ModelImporterClipAnimation[] clips = new ModelImporterClipAnimation[defaults.Length];
                for (int i = 0; i < defaults.Length; i++)
                {
                    clips[i] = defaults[i];
                    clips[i].name = i == 0 ? clipName : clipName + "_" + i;
                    clips[i].loopTime = loop;
                }

                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
        }

        private static Avatar loadAvatar(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
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

        private static void buildController(AnimatorController controller)
        {
            ensureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            ensureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
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

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            clearStateMachine(machine);

            Motion idle = loadFirstClip(IdleFbx);
            Motion walk = loadFirstClip(WalkFbx);
            Motion run = loadFirstClip(RunFbx);
            Motion attack = loadFirstClip(AttackFbx);
            Motion hit = loadFirstClip(HitFbx);
            Motion die = loadFirstClip(DieFbx);

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
                locomotion.AddChild(walk, 0.45f);
            }

            if (run != null)
            {
                locomotion.AddChild(run, 1f);
            }

            AnimatorState locomotionState = machine.AddState("Locomotion", new Vector3(300f, 0f, 0f));
            locomotionState.motion = locomotion;
            machine.defaultState = locomotionState;

            AnimatorState attackState = machine.AddState("Attack", new Vector3(300f, 120f, 0f));
            attackState.motion = attack;

            AnimatorState hitState = machine.AddState("Hit", new Vector3(520f, 120f, 0f));
            hitState.motion = hit;

            AnimatorState dieState = machine.AddState("Die", new Vector3(300f, 240f, 0f));
            dieState.motion = die;

            addTriggerTransition(locomotionState, attackState, "Attack", 0.1f);
            addAnyStateTransition(machine, hitState, "Hit", 0.05f);
            addAnyStateTransition(machine, dieState, "Die", 0.05f);

            AnimatorStateTransition attackToLocomotion = attackState.AddTransition(locomotionState);
            attackToLocomotion.hasExitTime = true;
            attackToLocomotion.exitTime = 0.9f;
            attackToLocomotion.duration = 0.15f;

            AnimatorStateTransition hitToLocomotion = hitState.AddTransition(locomotionState);
            hitToLocomotion.hasExitTime = true;
            hitToLocomotion.exitTime = 0.85f;
            hitToLocomotion.duration = 0.1f;

            // Die stays until despawn / pool reset.
        }

        private static void addTriggerTransition(
            AnimatorState from,
            AnimatorState to,
            string trigger,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void addAnyStateTransition(
            AnimatorStateMachine machine,
            AnimatorState to,
            string trigger,
            float duration)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
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

            AnimatorStateTransition[] any = stateMachine.anyStateTransitions;
            for (int i = any.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveAnyStateTransition(any[i]);
            }
        }

        private static Motion loadFirstClip(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning($"[Zombie War] Animation asset missing: {fbxPath}");
                return null;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                {
                    continue;
                }

                return clip;
            }

            return null;
        }

        private static void wirePrefab(AnimatorController controller)
        {
            GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshFbxPath);
            if (meshPrefab == null)
            {
                Debug.LogError($"[Zombie War] Mesh FBX missing: {MeshFbxPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform existingVisual = root.transform.Find(VisualChildName);
                if (existingVisual != null)
                {
                    Object.DestroyImmediate(existingVisual.gameObject);
                }

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(meshPrefab, root.transform);
                visual.name = VisualChildName;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                Animator animator = visual.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    animator = visual.AddComponent<Animator>();
                }

                Avatar avatar = loadAvatar(MeshFbxPath);
                if (avatar != null)
                {
                    animator.avatar = avatar;
                }

                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.runtimeAnimatorController = controller;

                MeshRenderer rootRenderer = root.GetComponent<MeshRenderer>();
                if (rootRenderer != null)
                {
                    rootRenderer.enabled = false;
                }

                ZombieWar.Enemy.ZombieAnimation zombieAnimation = root.GetComponent<ZombieWar.Enemy.ZombieAnimation>();
                if (zombieAnimation != null)
                {
                    SerializedObject so = new SerializedObject(zombieAnimation);
                    so.FindProperty("animator").objectReferenceValue = animator;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning("[Zombie War] ZombieAnimation missing on prefab root.");
                }

                ZombieWar.Enemy.ZombieAttack zombieAttack = root.GetComponent<ZombieWar.Enemy.ZombieAttack>();
                if (zombieAttack != null && zombieAnimation != null)
                {
                    SerializedObject attackSo = new SerializedObject(zombieAttack);
                    SerializedProperty animProp = attackSo.FindProperty("zombieAnimation");
                    if (animProp != null)
                    {
                        animProp.objectReferenceValue = zombieAnimation;
                        attackSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
