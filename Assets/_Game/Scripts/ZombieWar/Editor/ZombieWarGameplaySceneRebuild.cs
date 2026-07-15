#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieWar.Core;
using ZombieWar.Level;
using ZombieWar.Player;
using ZombieWar.UI;
using ZombieWar.Weapon;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Rebuilds the corrupt / empty Gameplay scene into a playable ZombieWar layout.
    /// Menu: Zombie War / Rebuild Gameplay Scene
    /// Also auto-runs once when Assets/_Game/EditorTemp/RebuildGameplay.request exists.
    /// </summary>
    public static class ZombieWarGameplaySceneRebuild
    {
        private const string MenuPath = "Zombie War/Rebuild Gameplay Scene";
        private const string ScenePath = "Assets/_SDK/Template/Scenes/Gameplay.unity";
        private const string RequestPath = "Assets/_Game/EditorTemp/RebuildGameplay.request";

        private const string SoldierConfigGuid = "5c16f929b391d824486b6c7bc42c0b91";
        private const string AugConfigGuid = "15f3eff39ab7f184f90c5236cf502685";
        private const string FamasConfigGuid = "a23f21c023852774aacadaa89cecec54";

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RequestPath) ?? "Assets/_Game/EditorTemp");
            File.WriteAllText(RequestPath, "rebuild");
            bool ok = rebuildInternal();
            if (ok)
            {
                tryDeleteRequest();
            }
        }

        private static bool isRebuildRunning;

        [InitializeOnLoadMethod]
        private static void autoRebuildIfRequested()
        {
            // One-shot only after domain reload. Do NOT poll every frame — that rewrote
            // LevelMapCatalog.asset while Unity still held the file lock (Access denied).
            EditorApplication.delayCall += tryAutoRebuildWhenReady;
        }

        private static void tryAutoRebuildWhenReady()
        {
            if (!File.Exists(RequestPath) || isRebuildRunning)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += tryAutoRebuildWhenReady;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("[Zombie War] Exiting Play Mode before Gameplay rebuild.");
                EditorApplication.delayCall += tryAutoRebuildWhenReady;
                return;
            }

            // Consume request first so a failed/partial rebuild cannot hammer SaveAssets.
            tryDeleteRequest();

            isRebuildRunning = true;
            try
            {
                bool ok = rebuildInternal();
                if (ok)
                {
                    Debug.Log("[Zombie War] Auto-rebuild finished (scene verified playable).");
                }
                else
                {
                    Debug.LogError(
                        "[Zombie War] Auto-rebuild incomplete. " +
                        "Run menu Zombie War / Rebuild Gameplay Scene once Unity is idle.");
                }
            }
            finally
            {
                isRebuildRunning = false;
            }
        }

        private static void tryDeleteRequest()
        {
            try
            {
                if (File.Exists(RequestPath))
                {
                    File.Delete(RequestPath);
                }

                string meta = RequestPath + ".meta";
                if (File.Exists(meta))
                {
                    File.Delete(meta);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Zombie War] Could not delete rebuild request: {ex.Message}");
            }
        }

        /// <returns>True when scene has the minimum playable hierarchy and was saved.</returns>
        private static bool rebuildInternal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                Directory.CreateDirectory(Path.GetDirectoryName(RequestPath) ?? "Assets/_Game/EditorTemp");
                File.WriteAllText(RequestPath, "rebuild");
                Debug.LogWarning("[Zombie War] Exiting Play Mode — rebuild will continue via request file.");
                return false;
            }

            // Force-open Gameplay — never block on "unsaved changes" dialog.
            EditorSceneManager.SaveOpenScenes();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            if (!scene.IsValid())
            {
                Debug.LogError($"[Zombie War] Failed to open {ScenePath}");
                return false;
            }

            clearNonCoreRoots();
            ensureDirectionalLight();
            ensureMainCamera();
            ensureEventSystem();

            GameObject player = null;
            runStep("Create player__root", () => player = ensurePlayerRoot());
            runStep("Wire joystick", () => wireJoystick(player));
            runStep("Bullet system", ensureBulletSystem);
            runStep("Wire weapons", () => wireWeapons(player));
            runStep("Enemy detect zone", () => ensureDetectZone(player));

            // Scene-only wiring. Do NOT rewrite LevelMapCatalog / wave SO here —
            // those assets already exist; concurrent SaveAssets caused Access denied.
            runStep("Setup Gameplay Waves + LevelMapBootstrap", ZombieWarGameplayWaveSceneSetup.SetupSceneOnly);
            runStep("Setup Wave Announce UI", ZombieWarWaveAnnounceSceneSetup.Setup);
            runStep("Setup Game Cameras", GameCameraSceneSetup.SetupGameCameras);
            runStep("Setup Win/Lose Flow", ZombieWarWinLoseSceneSetup.Setup);
            runStep("Fix Spawns And Bake NavMesh", ZombieWarSpawnAndNavMeshFix.Fix);

            // Ensure level map root + wave manager exist even if a later step failed earlier.
            ensureLevelBootstrapFallback();
            ensureWaveManagerFallback(player);

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            bool playable = isPlayableScene();

            int rootCount = scene.rootCount;
            Debug.Log(
                "[Zombie War] Gameplay scene rebuilt" + (saved ? " and saved" : " (save failed)") + ".\n" +
                $"- Scene: {ScenePath}\n" +
                $"- Roots: {rootCount}\n" +
                $"- Player: {(player != null ? player.name : "missing")}\n" +
                $"- Playable verify: {playable}\n" +
                "- Level1/2 Addressables + wave configs wired\n" +
                "Play: Menu → Play. Map loads via LevelMapBootstrap at runtime.");

            return saved && playable;
        }

        private static bool isPlayableScene()
        {
            bool hasPlayer = GameObject.Find("player__root") != null
                             || UnityEngine.Object.FindFirstObjectByType<PlayerHealth>() != null;
            bool hasWaves = UnityEngine.Object.FindFirstObjectByType<WaveManager>() != null;
            bool hasMap = UnityEngine.Object.FindFirstObjectByType<LevelMapBootstrap>() != null
                          || GameObject.Find("ZW_LevelMapRoot") != null;
            bool hasFlow = UnityEngine.Object.FindFirstObjectByType<ZombieWarGameFlow>() != null
                           || GameObject.Find("ZW_GameFlow") != null;

            if (!hasPlayer || !hasWaves || !hasMap || !hasFlow)
            {
                Debug.LogError(
                    "[Zombie War] Playable verify failed — " +
                    $"player={hasPlayer}, waves={hasWaves}, map={hasMap}, flow={hasFlow}");
                return false;
            }

            return true;
        }

        private static void runStep(string label, System.Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Zombie War] Step failed: {label}\n{ex}");
            }
        }

        private static void ensureLevelBootstrapFallback()
        {
            LevelMapCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelMapCatalog>(
                "Assets/_Game/Data/ZombieWar/Level/LevelMapCatalog.asset");
            if (catalog == null)
            {
                return;
            }

            LevelMapBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<LevelMapBootstrap>();
            if (bootstrap == null)
            {
                GameObject root = GameObject.Find("ZW_LevelMapRoot");
                if (root == null)
                {
                    root = new GameObject("ZW_LevelMapRoot");
                    Undo.RegisterCreatedObjectUndo(root, "Create LevelMapRoot");
                }

                bootstrap = root.GetComponent<LevelMapBootstrap>();
                if (bootstrap == null)
                {
                    bootstrap = Undo.AddComponent<LevelMapBootstrap>(root);
                }
            }

            WaveManager waveManager = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            SerializedObject so = new SerializedObject(bootstrap);
            setObjectRef(so, "catalog", catalog);
            setInt(so, "levelNumber", 1);
            setBool(so, "useProfileLevel", true);
            setObjectRef(so, "mapParent", bootstrap.transform);
            setBool(so, "loadOnEnable", true);
            setBool(so, "preloadNextLevel", true);
            setObjectRef(so, "waveManager", waveManager);
            setBool(so, "bakeNavMeshOnMapReady", true);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void setObjectRef(SerializedObject so, string property, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void setInt(SerializedObject so, string property, int value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.intValue = value;
            }
        }

        private static void setBool(SerializedObject so, string property, bool value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.boolValue = value;
            }
        }

        private static void ensureWaveManagerFallback(GameObject player)
        {
            WaveManager waveManager = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                GameObject go = new GameObject("wave___Manager");
                Undo.RegisterCreatedObjectUndo(go, "Create WaveManager");
                waveManager = Undo.AddComponent<WaveManager>(go);
            }

            LevelWaveConfig level1 = AssetDatabase.LoadAssetAtPath<LevelWaveConfig>(
                "Assets/_Game/Data/ZombieWar/Level/LevelWaveConfig_Level1.asset");
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;

            SerializedObject so = new SerializedObject(waveManager);
            if (level1 != null)
            {
                so.FindProperty("levelConfig").objectReferenceValue = level1;
            }

            if (player != null)
            {
                so.FindProperty("playerTarget").objectReferenceValue = player.transform;
            }

            if (health != null)
            {
                so.FindProperty("playerHealth").objectReferenceValue = health;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void clearNonCoreRoots()
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                string name = root.name;
                if (name == "Main Camera" || name == "Directional Light")
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ensureDirectionalLight()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.956f, 0.839f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Undo.RegisterCreatedObjectUndo(lightObject, "Create Directional Light");
        }

        private static void ensureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                cameraObject.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");
            }

            camera.transform.position = new Vector3(0f, 12f, -8f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        }

        private static void ensureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                eventSystem.AddComponent(inputModuleType);
            }
            else
            {
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static GameObject ensurePlayerRoot()
        {
            GameObject player = GameObject.Find("player__root");
            if (player == null)
            {
                player = new GameObject("player__root");
                Undo.RegisterCreatedObjectUndo(player, "Create player__root");
            }

            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0f, 0f);

            CapsuleCollider capsule = ensureComponent<CapsuleCollider>(player);
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            PlayerMovement movement = ensureComponent<PlayerMovement>(player);
            PlayerAnimation animation = ensureComponent<PlayerAnimation>(player);
            PlayerHealth health = ensureComponent<PlayerHealth>(player);
            EnemyTargetScanner scanner = ensureComponent<EnemyTargetScanner>(player);
            PlayerCombat combat = ensureComponent<PlayerCombat>(player);
            WeaponController weaponController = ensureComponent<WeaponController>(player);
            ProjectileWeapon projectileWeapon = ensureComponent<ProjectileWeapon>(player);
            ensureComponent<BulletProjectileSystem>(player);
            ensureComponent<DamageableHitboxResolver>(player);
            GunRecoil gunRecoil = ensureComponent<GunRecoil>(player);
            PlayerWeaponAttach weaponAttach = ensureComponent<PlayerWeaponAttach>(player);
            PlayerCharacterLoader characterLoader = ensureComponent<PlayerCharacterLoader>(player);
            ensureComponent<AudioSource>(player);

            Transform visualRoot = ensureChild(player.transform, "VisualRoot", Vector3.zero);

            SerializedObject movementSo = new SerializedObject(movement);
            movementSo.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            movementSo.FindProperty("targetScanner").objectReferenceValue = scanner;
            movementSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject animSo = new SerializedObject(animation);
            SerializedProperty healthProp = animSo.FindProperty("health");
            if (healthProp != null)
            {
                healthProp.objectReferenceValue = health;
            }

            animSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject combatSo = new SerializedObject(combat);
            combatSo.FindProperty("targetScanner").objectReferenceValue = scanner;
            combatSo.FindProperty("weaponController").objectReferenceValue = weaponController;
            combatSo.FindProperty("playerAnimation").objectReferenceValue = animation;
            combatSo.FindProperty("aimRoot").objectReferenceValue = player.transform;
            combatSo.FindProperty("playerMovement").objectReferenceValue = movement;
            combatSo.FindProperty("autoFire").boolValue = true;
            combatSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject attachSo = new SerializedObject(weaponAttach);
            attachSo.FindProperty("weaponController").objectReferenceValue = weaponController;
            attachSo.FindProperty("projectileWeapon").objectReferenceValue = projectileWeapon;
            attachSo.FindProperty("gunRecoil").objectReferenceValue = gunRecoil;
            attachSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject weaponCtrlSo = new SerializedObject(weaponController);
            weaponCtrlSo.FindProperty("weapon").objectReferenceValue = projectileWeapon;
            SerializedProperty gunRefs = weaponCtrlSo.FindProperty("gunConfigReferences");
            gunRefs.arraySize = 2;
            setAssetGuid(gunRefs.GetArrayElementAtIndex(0), AugConfigGuid);
            setAssetGuid(gunRefs.GetArrayElementAtIndex(1), FamasConfigGuid);
            weaponCtrlSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject loaderSo = new SerializedObject(characterLoader);
            loaderSo.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            loaderSo.FindProperty("movement").objectReferenceValue = movement;
            loaderSo.FindProperty("playerAnimation").objectReferenceValue = animation;
            loaderSo.FindProperty("playerWeaponAttachBehaviour").objectReferenceValue = weaponAttach;
            setAssetGuid(loaderSo.FindProperty("characterConfigReference"), SoldierConfigGuid);
            loaderSo.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static void wireJoystick(GameObject player)
        {
            // Reuse joystick canvas builder from Basic3DJoystick by calling public Setup
            // after temporarily ensuring player exists — then pin joystick onto player__root.
            GameObject canvasObject = GameObject.Find("ZW_JoystickCanvas");
            if (canvasObject == null)
            {
                // Create via Basic setup helpers by invoking private CreateJoystickCanvas through SetupScene
                // is too coarse (adds box player). Build joystick UI here.
                canvasObject = new GameObject(
                    "ZW_JoystickCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Joystick Canvas");

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            GameObject joystickObject = GameObject.Find("ZW_FixedJoystick");
            if (joystickObject == null)
            {
                joystickObject = createFixedJoystick(canvasObject.transform);
            }

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            SerializedObject movementSo = new SerializedObject(movement);
            movementSo.FindProperty("joystick").objectReferenceValue = joystickObject.GetComponent<Joystick>();
            movementSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject createFixedJoystick(Transform parent)
        {
            GameObject root = new GameObject("ZW_FixedJoystick", typeof(RectTransform), typeof(FixedJoystick));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(190f, 190f);
            rootRect.sizeDelta = new Vector2(220f, 220f);

            GameObject background = createUiImage("Background", root.transform, new Color(0f, 0f, 0f, 0.35f));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject handle = createUiImage("Handle", background.transform, new Color(1f, 1f, 1f, 0.65f));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(90f, 90f);

            FixedJoystick fixedJoystick = root.GetComponent<FixedJoystick>();
            SerializedObject joystickSo = new SerializedObject(fixedJoystick);
            joystickSo.FindProperty("background").objectReferenceValue = backgroundRect;
            joystickSo.FindProperty("handle").objectReferenceValue = handleRect;
            joystickSo.FindProperty("handleRange").floatValue = 1f;
            joystickSo.FindProperty("deadZone").floatValue = 0.05f;
            joystickSo.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Fixed Joystick");
            return root;
        }

        private static GameObject createUiImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            imageObject.GetComponent<Image>().color = color;
            return imageObject;
        }

        private static void ensureBulletSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<BulletProjectileSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("ZW_BulletProjectileSystem");
            go.AddComponent<BulletProjectileSystem>();
            Undo.RegisterCreatedObjectUndo(go, "Create BulletProjectileSystem");
        }

        private static void wireWeapons(GameObject player)
        {
            ProjectileWeapon projectileWeapon = player.GetComponent<ProjectileWeapon>();
            BulletProjectileSystem bulletSystem = UnityEngine.Object.FindFirstObjectByType<BulletProjectileSystem>();
            Transform firePoint = ensureChild(player.transform, "ZW_FirePoint", new Vector3(0f, 1.4f, 0.6f));

            SerializedObject so = new SerializedObject(projectileWeapon);
            so.FindProperty("firePoint").objectReferenceValue = firePoint;
            so.FindProperty("bulletSystem").objectReferenceValue = bulletSystem;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject muzzle = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Vfx/ZombieWar/MuzzleFlashVfx.prefab");
            if (muzzle != null)
            {
                so = new SerializedObject(projectileWeapon);
                so.FindProperty("defaultMuzzleVfxPrefab").objectReferenceValue = muzzle;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject bullet = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Vfx/ZombieWar/BulletTracer.prefab");
            if (bullet == null)
            {
                bullet = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Game/Prefabs/ZombieWar/BulletTracer.prefab");
            }

            if (bullet != null)
            {
                so = new SerializedObject(projectileWeapon);
                so.FindProperty("fallbackBulletPrefab").objectReferenceValue = bullet;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ensureDetectZone(GameObject player)
        {
            Transform existing = player.transform.Find("EnemyDetectZone");
            GameObject zoneObject;
            if (existing != null)
            {
                zoneObject = existing.gameObject;
            }
            else
            {
                zoneObject = new GameObject("EnemyDetectZone");
                zoneObject.transform.SetParent(player.transform, false);
                zoneObject.transform.localPosition = new Vector3(0f, 1f, 0f);
                Undo.RegisterCreatedObjectUndo(zoneObject, "Create EnemyDetectZone");
            }

            SphereCollider sphere = ensureComponent<SphereCollider>(zoneObject);
            sphere.isTrigger = true;
            sphere.radius = 12f;

            EnemyDetectZone detectZone = ensureComponent<EnemyDetectZone>(zoneObject);
            EnemyTargetScanner scanner = player.GetComponent<EnemyTargetScanner>();

            if (scanner == null)
            {
                Debug.LogError("[Zombie War] EnemyTargetScanner missing on player.");
                return;
            }

            SerializedObject zoneSo = new SerializedObject(detectZone);
            zoneSo.FindProperty("scanner").objectReferenceValue = scanner;
            zoneSo.FindProperty("detectCollider").objectReferenceValue = sphere;
            zoneSo.FindProperty("radius").floatValue = 12f;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject scannerSo = new SerializedObject(scanner);
            SerializedProperty zoneProp = scannerSo.FindProperty("detectZone");
            if (zoneProp != null)
            {
                zoneProp.objectReferenceValue = detectZone;
                scannerSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Transform ensureChild(Transform parent, string childName, Vector3 localPosition)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                existing.localPosition = localPosition;
                return existing;
            }

            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            Undo.RegisterCreatedObjectUndo(child, "Create " + childName);
            return child.transform;
        }

        private static T ensureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = Undo.AddComponent<T>(go);
            }

            return component;
        }

        private static void setAssetGuid(SerializedProperty assetReferenceProperty, string guid)
        {
            if (assetReferenceProperty == null)
            {
                return;
            }

            SerializedProperty guidProp = assetReferenceProperty.FindPropertyRelative("m_AssetGUID");
            if (guidProp != null)
            {
                guidProp.stringValue = guid;
            }
        }
    }
}
#endif
