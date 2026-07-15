#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.Level;
using ZombieWar.Player;
using ZombieWar.UI;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// One-click Gameplay wiring: wave configs, spawn points, WaveManager,
    /// LevelMapBootstrap, GameFlow, WaveAnnouncePresenter.
    /// Menu: Zombie War / Setup Gameplay Waves
    /// </summary>
    public static class ZombieWarGameplayWaveSceneSetup
    {
        private const string MenuPath = "Zombie War/Setup Gameplay Waves";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            ZombieWarLevelMapAddressableSetup.Setup(skipSceneWire: true);
            ZombieWarLevelWaveConfigSetup.Setup();
            SetupSceneOnly();
            EditorSceneManager.SaveOpenScenes();
        }

        /// <summary>
        /// Wires scene objects only — does not rewrite LevelMapCatalog / wave SO assets.
        /// </summary>
        public static void SetupSceneOnly()
        {
            LevelWaveConfig level1Waves = AssetDatabase.LoadAssetAtPath<LevelWaveConfig>(
                "Assets/_Game/Data/ZombieWar/Level/LevelWaveConfig_Level1.asset");
            LevelMapCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelMapCatalog>(
                "Assets/_Game/Data/ZombieWar/Level/LevelMapCatalog.asset");

            if (level1Waves == null)
            {
                Debug.LogError("[Zombie War] LevelWaveConfig_Level1 missing. Run Zombie War/Level/Setup Wave Configs.");
                return;
            }

            if (catalog == null)
            {
                Debug.LogError("[Zombie War] LevelMapCatalog missing. Run Zombie War/Addressables/Setup Level Maps.");
                return;
            }

            PlayerHealth playerHealth = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            Transform playerTarget = resolvePlayerTarget(playerHealth);
            if (playerHealth == null || playerTarget == null)
            {
                Debug.LogError("[Zombie War] PlayerHealth / player target not found in scene.");
                return;
            }

            WaveManager waveManager = ensureWaveManager();
            ZombieSpawnPoint[] spawnPoints = ensureSpawnPoints(waveManager.transform, playerTarget.position);

            SerializedObject waveSo = new SerializedObject(waveManager);
            waveSo.FindProperty("levelConfig").objectReferenceValue = level1Waves;
            SerializedProperty spawnProp = waveSo.FindProperty("spawnPoints");
            spawnProp.arraySize = spawnPoints.Length;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                spawnProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }

            waveSo.FindProperty("playerTarget").objectReferenceValue = playerTarget;
            waveSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            waveSo.ApplyModifiedPropertiesWithoutUndo();

            wireAnnouncePresenter(waveManager);
            LevelMapBootstrap bootstrap = ensureLevelMapBootstrap(catalog, waveManager);
            wireGameFlow(waveManager, bootstrap, playerHealth);

            removeBakedMapRoot("level1");
            removeBakedMapRoot("level2");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = waveManager.gameObject;

            Debug.Log(
                "[Zombie War] Gameplay waves scene-wired.\n" +
                $"- WaveManager: {waveManager.name}\n" +
                $"- LevelWaveConfig: {level1Waves.name}\n" +
                $"- Spawn points: {spawnPoints.Length}\n" +
                $"- Player: {playerTarget.name}\n" +
                "- LevelMapBootstrap loads Level1 Addressable map\n" +
                "- GameFlow waits for map then StartWaves");
        }

        private static Transform resolvePlayerTarget(PlayerHealth playerHealth)
        {
            if (playerHealth == null)
            {
                return null;
            }

            Transform root = playerHealth.transform;
            if (root.name.Contains("player"))
            {
                return root;
            }

            GameObject named = GameObject.Find("player__root");
            return named != null ? named.transform : root;
        }

        private static WaveManager ensureWaveManager()
        {
            WaveManager existing = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject("wave___Manager");
            Undo.RegisterCreatedObjectUndo(go, "Create WaveManager");
            return Undo.AddComponent<WaveManager>(go);
        }

        private static ZombieSpawnPoint[] ensureSpawnPoints(Transform waveRoot, Vector3 playerPos)
        {
            Transform pointsRoot = waveRoot.Find("SpawnPoints");
            if (pointsRoot == null)
            {
                GameObject rootGo = new GameObject("SpawnPoints");
                Undo.RegisterCreatedObjectUndo(rootGo, "Create SpawnPoints");
                rootGo.transform.SetParent(waveRoot, false);
                pointsRoot = rootGo.transform;
            }

            Vector3[] offsets =
            {
                new Vector3(-18f, 0f, 12f),
                new Vector3(18f, 0f, 12f),
                new Vector3(-14f, 0f, -16f),
                new Vector3(14f, 0f, -16f),
            };

            ZombieSpawnPoint[] points = new ZombieSpawnPoint[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                string name = $"SpawnPoint_{i + 1}";
                Transform child = pointsRoot.Find(name);
                GameObject pointGo;
                if (child != null)
                {
                    pointGo = child.gameObject;
                }
                else
                {
                    pointGo = new GameObject(name);
                    Undo.RegisterCreatedObjectUndo(pointGo, "Create SpawnPoint");
                    pointGo.transform.SetParent(pointsRoot, false);
                }

                Vector3 world = playerPos + offsets[i];
                world.y = playerPos.y;
                pointGo.transform.position = world;

                ZombieSpawnPoint spawnPoint = pointGo.GetComponent<ZombieSpawnPoint>();
                if (spawnPoint == null)
                {
                    spawnPoint = Undo.AddComponent<ZombieSpawnPoint>(pointGo);
                }

                points[i] = spawnPoint;
            }

            return points;
        }

        private static void wireAnnouncePresenter(WaveManager waveManager)
        {
            WaveAnnouncePresenter presenter = UnityEngine.Object.FindFirstObjectByType<WaveAnnouncePresenter>();
            if (presenter == null)
            {
                GameObject canvas = GameObject.Find("ZW_WaveAnnounceCanvas");
                if (canvas != null)
                {
                    presenter = canvas.GetComponent<WaveAnnouncePresenter>();
                }
            }

            if (presenter == null)
            {
                Debug.LogWarning("[Zombie War] WaveAnnouncePresenter missing. Run Setup Wave Announce UI first.");
                return;
            }

            SerializedObject so = new SerializedObject(presenter);
            so.FindProperty("waveManager").objectReferenceValue = waveManager;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LevelMapBootstrap ensureLevelMapBootstrap(LevelMapCatalog catalog, WaveManager waveManager)
        {
            LevelMapBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<LevelMapBootstrap>();
            GameObject root;
            if (bootstrap != null)
            {
                root = bootstrap.gameObject;
            }
            else
            {
                root = GameObject.Find("ZW_LevelMapRoot");
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

            SerializedObject so = new SerializedObject(bootstrap);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("levelNumber").intValue = 1;
            so.FindProperty("mapParent").objectReferenceValue = root.transform;
            so.FindProperty("loadOnEnable").boolValue = true;
            so.FindProperty("preloadNextLevel").boolValue = true;
            so.FindProperty("waveManager").objectReferenceValue = waveManager;
            so.ApplyModifiedPropertiesWithoutUndo();
            return bootstrap;
        }

        private static void wireGameFlow(
            WaveManager waveManager,
            LevelMapBootstrap bootstrap,
            PlayerHealth playerHealth)
        {
            ZombieWarGameFlow flow = UnityEngine.Object.FindFirstObjectByType<ZombieWarGameFlow>();
            if (flow == null)
            {
                GameObject go = new GameObject("ZW_GameFlow");
                Undo.RegisterCreatedObjectUndo(go, "Create GameFlow");
                flow = Undo.AddComponent<ZombieWarGameFlow>(go);
            }

            SerializedObject so = new SerializedObject(flow);
            so.FindProperty("waveManager").objectReferenceValue = waveManager;
            so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("levelMapBootstrap").objectReferenceValue = bootstrap;
            so.FindProperty("startWavesOnPlay").boolValue = true;
            so.FindProperty("waitForMapBeforeStart").boolValue = true;
            so.FindProperty("pauseTimeOnFinish").boolValue = true;
            so.FindProperty("autoCreateResultUi").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void removeBakedMapRoot(string name)
        {
            GameObject baked = GameObject.Find(name);
            if (baked == null || baked.transform.parent != null)
            {
                return;
            }

            // Skip if this is somehow under LevelMapRoot already.
            if (baked.GetComponentInParent<LevelMapBootstrap>() != null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(baked);
            Debug.Log($"[Zombie War] Removed baked scene map '{name}' (Addressable load will recreate).");
        }
    }
}
#endif
