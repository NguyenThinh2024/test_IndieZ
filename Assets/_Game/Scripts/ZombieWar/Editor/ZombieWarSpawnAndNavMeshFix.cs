#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using ZombieWar.Level;
using ZombieWar.Player;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Bake NavMesh from current map geometry and snap WaveManager spawn points onto walkable ground.
    /// Menu: Zombie War / Fix Spawns And Bake NavMesh
    /// </summary>
    public static class ZombieWarSpawnAndNavMeshFix
    {
        private const string MenuPath = "Zombie War/Fix Spawns And Bake NavMesh";
        private const string Level1PrefabPath = "Assets/_Game/Prefabs/ZombieWar/Levels/level1.prefab";
        private const float SpawnRadius = 28f;
        private const float SampleRadius = 12f;

        [MenuItem(MenuPath)]
        public static void Fix()
        {
            PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogError("[Zombie War] PlayerHealth not found.");
                return;
            }

            Transform player = playerHealth.transform;
            WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogError("[Zombie War] WaveManager not found. Run Setup Gameplay Waves first.");
                return;
            }

            LevelMapBootstrap bootstrap = Object.FindFirstObjectByType<LevelMapBootstrap>();
            ensureMapPresentForBake(bootstrap);

            ZombieWarNavMeshBakeSetup.SetupAndBakeNavMesh();

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (bootstrap != null && surface != null)
            {
                SerializedObject bootSo = new SerializedObject(bootstrap);
                bootSo.FindProperty("navMeshSurface").objectReferenceValue = surface;
                bootSo.FindProperty("bakeNavMeshOnMapReady").boolValue = true;
                bootSo.ApplyModifiedPropertiesWithoutUndo();
            }

            ZombieSpawnPoint[] points = placeAndSnapSpawnPoints(waveManager.transform, player.position);
            wireSpawnPoints(waveManager, points);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log(
                "[Zombie War] Spawns + NavMesh fixed.\n" +
                $"- Spawn points: {points.Length} snapped around player at ~{SpawnRadius}m\n" +
                "- NavMesh rebaked from current map\n" +
                "- LevelMapBootstrap will rebake again after Addressable map load at runtime");
        }

        private static void ensureMapPresentForBake(LevelMapBootstrap bootstrap)
        {
            if (bootstrap != null && bootstrap.transform.childCount > 0)
            {
                return;
            }

            GameObject existing = GameObject.Find("level1");
            if (existing != null)
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Level1PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[Zombie War] level1 prefab missing for editor bake preview.");
                return;
            }

            Transform parent = bootstrap != null ? bootstrap.transform : null;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate level1 for NavMesh bake");
            instance.name = "level1";
            Debug.Log("[Zombie War] Instantiated level1 for NavMesh bake.");
        }

        private static ZombieSpawnPoint[] placeAndSnapSpawnPoints(Transform waveRoot, Vector3 playerPos)
        {
            Transform pointsRoot = waveRoot.Find("SpawnPoints");
            if (pointsRoot == null)
            {
                GameObject rootGo = new GameObject("SpawnPoints");
                Undo.RegisterCreatedObjectUndo(rootGo, "Create SpawnPoints");
                rootGo.transform.SetParent(waveRoot, false);
                pointsRoot = rootGo.transform;
            }

            const int count = 4;
            ZombieSpawnPoint[] points = new ZombieSpawnPoint[count];
            for (int i = 0; i < count; i++)
            {
                string name = $"SpawnPoint_{i + 1}";
                Transform child = pointsRoot.Find(name);
                GameObject pointGo = child != null
                    ? child.gameObject
                    : new GameObject(name);

                if (child == null)
                {
                    Undo.RegisterCreatedObjectUndo(pointGo, "Create SpawnPoint");
                    pointGo.transform.SetParent(pointsRoot, false);
                }

                float angle = (i / (float)count) * Mathf.PI * 2f + 0.35f;
                Vector3 flatDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                if (!tryFindWalkableSpawn(playerPos, flatDir, out Vector3 snapped))
                {
                    snapped = playerPos + flatDir * SpawnRadius;
                    Debug.LogError($"[Zombie War] SpawnPoint_{i + 1} could not snap near player ground.", pointGo);
                }

                pointGo.transform.position = snapped;

                ZombieSpawnPoint spawnPoint = pointGo.GetComponent<ZombieSpawnPoint>();
                if (spawnPoint == null)
                {
                    spawnPoint = Undo.AddComponent<ZombieSpawnPoint>(pointGo);
                }

                points[i] = spawnPoint;
            }

            return points;
        }

        /// <summary>
        /// Walk outward from player and prefer NavMesh hits near player height
        /// (avoids snapping onto roofs / other elevation islands).
        /// </summary>
        private static bool tryFindWalkableSpawn(Vector3 playerPos, Vector3 flatDir, out Vector3 result)
        {
            result = playerPos;
            const float maxYDelta = 2.5f;
            const float minDistance = 16f;
            const float maxDistance = 34f;
            const float step = 2f;
            const float sample = 2.5f;

            Vector3 best = default;
            float bestScore = float.MaxValue;
            bool found = false;

            for (float distance = minDistance; distance <= maxDistance; distance += step)
            {
                Vector3 probe = playerPos + flatDir * distance;
                if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, sample, NavMesh.AllAreas))
                {
                    continue;
                }

                float yDelta = Mathf.Abs(hit.position.y - playerPos.y);
                if (yDelta > maxYDelta)
                {
                    continue;
                }

                float xz = Vector3.Distance(
                    new Vector3(playerPos.x, 0f, playerPos.z),
                    new Vector3(hit.position.x, 0f, hit.position.z));
                float score = Mathf.Abs(xz - SpawnRadius) + yDelta;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = hit.position;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            result = best;
            return true;
        }

        private static void wireSpawnPoints(WaveManager waveManager, ZombieSpawnPoint[] points)
        {
            SerializedObject waveSo = new SerializedObject(waveManager);
            SerializedProperty spawnProp = waveSo.FindProperty("spawnPoints");
            spawnProp.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                spawnProp.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }

            waveSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
