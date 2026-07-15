#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using ZombieWar.Enemy;
using ZombieWar.Level;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Builds Shirtless Elite enemy from NewPunch ShirtlessZombieFree mesh,
    /// registers Addressables, and wires WAVE 3 on Level 2 to spawn them as a boss pack.
    /// Menu: Zombie War/Enemy/Setup Shirtless Boss Elite
    /// </summary>
    public static class ZombieWarShirtlessBossSetup
    {
        private const string MenuPath = "Zombie War/Enemy/Setup Shirtless Boss Elite";

        private const string MeshFbxPath =
            "Assets/NewPunch/ShirtlessZombieFree/Models/ShirtlessZombie_FREE.fbx";
        private const string BodyMatPath =
            "Assets/NewPunch/ShirtlessZombieFree/Materials/URP/ZombieBB_Body_URP.mat";
        private const string ClothesMatPath =
            "Assets/NewPunch/ShirtlessZombieFree/Materials/URP/ZombieBB_Clothes_URP.mat";

        private const string TemplatePrefabPath = "Assets/_Game/Resources/ZombieWar/Zombie/Zombie.prefab";
        private const string PrefabFolder = "Assets/_Game/Resources/ZombieWar/Zombie";
        private const string PrefabPath = PrefabFolder + "/ShirtlessBoss.prefab";
        private const string ControllerPath = "Assets/_Game/Art/Animations/Zombie/Zombie.controller";

        private const string ConfigPath =
            "Assets/_Game/Addressables/Configs/Enemy/ZombieShirtlessBossConfig.json";
        private const string ConfigAddress = "ZombieWar/Enemy/Configs/ZombieShirtlessBoss";
        private const string PrefabAddress = "ZombieWar/Enemy/ShirtlessBoss";

        private const string Level2WavePath =
            "Assets/_Game/Data/ZombieWar/Level/LevelWaveConfig_Level2.asset";

        private const string VisualChildName = "ShirtlessVisual";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            if (!ensureMeshReady())
            {
                return;
            }

            if (!buildPrefab())
            {
                return;
            }

            if (!File.Exists(Path.GetFullPath(ConfigPath)))
            {
                Debug.LogError($"[Zombie War] Missing config JSON: {ConfigPath}");
                return;
            }

            registerAddressables();
            wireLevel2BossWave();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log(
                "[Zombie War] Shirtless Elite ready.\n" +
                $"- Prefab: {PrefabPath} → {PrefabAddress}\n" +
                $"- Config: {ConfigPath} → {ConfigAddress}\n" +
                "- Stats: higher HP / damage, flank surround AI\n" +
                "- Level 2 WAVE 3 spawns Shirtless Elite pack (boss announce)");
        }

        private static bool ensureMeshReady()
        {
            ModelImporter importer = AssetImporter.GetAtPath(MeshFbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[Zombie War] Shirtless mesh missing: {MeshFbxPath}");
                return false;
            }

            bool dirty = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                dirty = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }

            return true;
        }

        private static bool buildPrefab()
        {
            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath);
            GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshFbxPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (template == null)
            {
                Debug.LogError($"[Zombie War] Template zombie prefab missing: {TemplatePrefabPath}");
                return false;
            }

            if (meshPrefab == null)
            {
                Debug.LogError($"[Zombie War] Shirtless mesh prefab missing: {MeshFbxPath}");
                return false;
            }

            if (controller == null)
            {
                Debug.LogError(
                    $"[Zombie War] Animator controller missing: {ControllerPath}. " +
                    "Run 'Zombie War/Animation/Setup Zombie Animator' first.");
                return false;
            }

            ensureFolder(PrefabFolder);

            GameObject root = PrefabUtility.InstantiatePrefab(template) as GameObject;
            if (root == null)
            {
                root = Object.Instantiate(template);
            }

            root.name = "ShirtlessBoss";

            try
            {
                removeTemplateVisual(root.transform);

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(meshPrefab, root.transform);
                if (visual == null)
                {
                    visual = Object.Instantiate(meshPrefab, root.transform);
                }

                visual.name = VisualChildName;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                setLayerRecursive(visual, root.layer);

                applyUrpMaterials(visual);
                wireAnimator(visual, controller);
                wireGameplayRefs(root, visual);
                tuneAgent(root);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        }

        private static void removeTemplateVisual(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                string name = child.name;
                if (name == "ZombieVisual"
                    || name == VisualChildName
                    || name.Contains("Zombie")
                    || name.Contains("Shirtless"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void applyUrpMaterials(GameObject visual)
        {
            Material body = AssetDatabase.LoadAssetAtPath<Material>(BodyMatPath);
            Material clothes = AssetDatabase.LoadAssetAtPath<Material>(ClothesMatPath);
            if (body == null && clothes == null)
            {
                return;
            }

            SkinnedMeshRenderer[] renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                string rendererName = renderer.name.ToLowerInvariant();
                if (clothes != null && rendererName.Contains("cloth"))
                {
                    renderer.sharedMaterial = clothes;
                }
                else if (body != null)
                {
                    renderer.sharedMaterial = body;
                }
            }
        }

        private static void wireAnimator(GameObject visual, AnimatorController controller)
        {
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
        }

        private static void wireGameplayRefs(GameObject root, GameObject visual)
        {
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            ZombieAnimation zombieAnimation = root.GetComponent<ZombieAnimation>();
            if (zombieAnimation != null && animator != null)
            {
                SerializedObject so = new SerializedObject(zombieAnimation);
                so.FindProperty("animator").objectReferenceValue = animator;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            ZombieDissolve dissolve = root.GetComponent<ZombieDissolve>();
            if (dissolve != null)
            {
                Renderer[] all = visual.GetComponentsInChildren<Renderer>(true);
                SerializedObject so = new SerializedObject(dissolve);
                SerializedProperty renderers = so.FindProperty("renderers");
                renderers.arraySize = all.Length;
                for (int i = 0; i < all.Length; i++)
                {
                    renderers.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Keep model multi-material look — do not force single skin via ZombieVisualSkin.
            ZombieVisualSkin visualSkin = root.GetComponent<ZombieVisualSkin>();
            if (visualSkin != null)
            {
                SerializedObject so = new SerializedObject(visualSkin);
                so.FindProperty("renderers").arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void tuneAgent(GameObject root)
        {
            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                return;
            }

            agent.speed = 4.4f;
            agent.acceleration = 16f;
            agent.angularSpeed = 420f;
            agent.radius = 0.45f;
            agent.height = 1.9f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        private static void registerAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[Zombie War] Addressables settings could not be created.");
                return;
            }

            setupAddressableEntry(settings, PrefabPath, PrefabAddress);
            setupAddressableEntry(settings, ConfigPath, ConfigAddress);
            EditorUtility.SetDirty(settings);
        }

        private static void wireLevel2BossWave()
        {
            LevelWaveConfig level2 = AssetDatabase.LoadAssetAtPath<LevelWaveConfig>(Level2WavePath);
            if (level2 == null)
            {
                Debug.LogWarning(
                    $"[Zombie War] {Level2WavePath} missing. Run 'Zombie War/Level/Setup Wave Configs' then re-run this menu.");
                return;
            }

            string bossGuid = AssetDatabase.AssetPathToGUID(ConfigPath);
            if (string.IsNullOrEmpty(bossGuid))
            {
                Debug.LogError($"[Zombie War] Could not resolve GUID for {ConfigPath}");
                return;
            }

            SerializedObject so = new SerializedObject(level2);
            SerializedProperty waves = so.FindProperty("waves");
            if (waves == null)
            {
                return;
            }

            // Prefer the last announced wave as boss; keep exactly 3 sequential waves.
            if (waves.arraySize != 3)
            {
                waves.arraySize = 3;
            }

            writeWave(
                waves.GetArrayElementAtIndex(2),
                startTime: 4.5f,
                interval: 0.55f,
                spawnCount: 16,
                maxAlive: 10,
                displayName: "SHIRTLESS ELITE",
                isBoss: true,
                announceEnabled: true,
                announceLead: 4.5f,
                configGuid: bossGuid,
                announceSubtitle: "Prepare for the boss!");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level2);
        }

        private static void writeWave(
            SerializedProperty wave,
            float startTime,
            float interval,
            int spawnCount,
            int maxAlive,
            string displayName,
            bool isBoss,
            bool announceEnabled,
            float announceLead,
            string configGuid,
            string announceSubtitle = null)
        {
            wave.FindPropertyRelative("startTime").floatValue = startTime;
            wave.FindPropertyRelative("spawnInterval").floatValue = interval;
            wave.FindPropertyRelative("spawnCount").intValue = spawnCount;
            wave.FindPropertyRelative("maxAlive").intValue = maxAlive;
            wave.FindPropertyRelative("displayName").stringValue = displayName;
            SerializedProperty subtitle = wave.FindPropertyRelative("announceSubtitle");
            if (subtitle != null)
            {
                subtitle.stringValue = announceSubtitle ?? string.Empty;
            }

            wave.FindPropertyRelative("isBoss").boolValue = isBoss;
            wave.FindPropertyRelative("announceEnabled").boolValue = announceEnabled;
            wave.FindPropertyRelative("announceLeadSeconds").floatValue = announceLead;
            wave.FindPropertyRelative("zombiePrefab").objectReferenceValue = null;
            wave.FindPropertyRelative("zombieConfigReference")
                .FindPropertyRelative("m_AssetGUID").stringValue = configGuid;
        }

        private static void setupAddressableEntry(
            AddressableAssetSettings settings,
            string assetPath,
            string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[Zombie War] Addressable asset missing: {assetPath}");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetAddress(address);
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

        private static void setLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                setLayerRecursive(t.GetChild(i).gameObject, layer);
            }
        }

        private static void ensureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            ensureFolder(parent);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
