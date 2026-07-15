#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using ZombieWar.Player;

namespace ZombieWar.EditorTools
{
    public static class ZombieWarNavMeshBakeSetup
    {
        private const string MenuSetupPath = "Zombie War/Setup And Bake NavMesh";
        private const string MenuRebakePath = "Zombie War/Rebake NavMesh";
        private const string NavigationObjectName = "ZW_Navigation";

        [MenuItem(MenuSetupPath)]
        public static void SetupAndBakeNavMesh()
        {
            NavMeshSurface surface = ensureNavigationSurface();
            if (surface == null)
            {
                return;
            }

            bakeSurface(surface);
            validateAfterBake(surface);
            markActiveSceneDirty();
        }

        [MenuItem(MenuRebakePath)]
        public static void RebakeNavMesh()
        {
            NavMeshSurface surface = findNavigationSurface();
            if (surface == null)
            {
                return;
            }

            bakeSurface(surface);
            validateAfterBake(surface);
            markActiveSceneDirty();
        }

        private static NavMeshSurface ensureNavigationSurface()
        {
            GameObject navigationObject = GameObject.Find(NavigationObjectName);
            if (navigationObject == null)
            {
                navigationObject = new GameObject(NavigationObjectName);
                Undo.RegisterCreatedObjectUndo(navigationObject, "Create Navigation Surface");
            }

            NavMeshSurface surface = navigationObject.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = Undo.AddComponent<NavMeshSurface>(navigationObject);
            }

            surface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.defaultArea = 0;
            surface.layerMask = ~0;
            surface.overrideTileSize = false;
            surface.overrideVoxelSize = false;

            EditorUtility.SetDirty(surface);
            return surface;
        }

        private static NavMeshSurface findNavigationSurface()
        {
            GameObject navigationObject = GameObject.Find(NavigationObjectName);
            return navigationObject != null ? navigationObject.GetComponent<NavMeshSurface>() : null;
        }

        private static void bakeSurface(NavMeshSurface surface)
        {
            surface.RemoveData();
            surface.BuildNavMesh();
        }

        private static void validateAfterBake(NavMeshSurface surface)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices == null || triangulation.vertices.Length == 0)
            {
                Debug.LogError(
                    "[Zombie War] NavMesh bake produced zero triangles. Add floor/terrain meshes to the scene, then rebake.",
                    surface);
                return;
            }

            snapWaveSpawnPoints();
        }

        private static void snapWaveSpawnPoints()
        {
            ZombieWar.Level.WaveManager waveManager =
                Object.FindFirstObjectByType<ZombieWar.Level.WaveManager>();
            if (waveManager == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(waveManager);
            SerializedProperty arr = so.FindProperty("spawnPoints");
            for (int i = 0; i < arr.arraySize; i++)
            {
                ZombieWar.Level.ZombieSpawnPoint spawnPoint =
                    arr.GetArrayElementAtIndex(i).objectReferenceValue as ZombieWar.Level.ZombieSpawnPoint;
                if (spawnPoint == null)
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(spawnPoint.Position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    continue;
                }

                PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
                if (playerHealth != null
                    && Mathf.Abs(hit.position.y - playerHealth.transform.position.y) > 2.5f)
                {
                    continue;
                }

                if ((hit.position - spawnPoint.Position).sqrMagnitude > 0.25f)
                {
                    Undo.RecordObject(spawnPoint.transform, "Snap Spawn To NavMesh");
                    spawnPoint.transform.position = hit.position;
                    EditorUtility.SetDirty(spawnPoint.transform);
                }
            }
        }

        private static void markActiveSceneDirty()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }
    }
}
#endif
