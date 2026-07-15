#if UNITY_EDITOR
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Bakes NavMesh into a level map prefab (NavMeshSurface + NavMeshData sub-asset).
    /// </summary>
    public static class ZombieWarLevelPrefabNavMeshBake
    {
        private const string LevelsFolder = "Assets/_Game/Prefabs/ZombieWar/Levels";
        private const string Level1Path = LevelsFolder + "/level1.prefab";
        private const string Level2Path = LevelsFolder + "/level2.prefab";

        [MenuItem("Zombie War/Level/Bake NavMesh Into Level2 Prefab")]
        public static void BakeLevel2()
        {
            BakePrefab(Level2Path);
        }

        [MenuItem("Zombie War/Level/Bake NavMesh Into Level1 Prefab")]
        public static void BakeLevel1()
        {
            BakePrefab(Level1Path);
        }

        [MenuItem("Zombie War/Level/Bake NavMesh Into All Level Prefabs")]
        public static void BakeAll()
        {
            BakePrefab(Level1Path);
            BakePrefab(Level2Path);
        }

        public static bool BakePrefab(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath) || !File.Exists(prefabPath))
            {
                Debug.LogError($"[Zombie War] Level prefab missing: {prefabPath}");
                return false;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[Zombie War] Could not load prefab: {prefabPath}");
                return false;
            }

            clearExistingNavMeshDataAssets(prefabPath);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            if (instance == null)
            {
                Debug.LogError($"[Zombie War] Failed to instantiate: {prefabPath}");
                return false;
            }

            instance.name = Path.GetFileNameWithoutExtension(prefabPath);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;

            try
            {
                NavMeshSurface surface = instance.GetComponent<NavMeshSurface>();
                if (surface == null)
                {
                    surface = instance.AddComponent<NavMeshSurface>();
                }

                surface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                surface.defaultArea = 0;
                surface.layerMask = ~0;
                surface.overrideTileSize = false;
                surface.overrideVoxelSize = false;

                surface.RemoveData();
                surface.BuildNavMesh();

                if (surface.navMeshData == null)
                {
                    Debug.LogError(
                        $"[Zombie War] NavMesh bake failed for '{instance.name}' (no NavMeshData). Check floor meshes/colliders.",
                        instance);
                    return false;
                }

                // Persist NavMeshData as a sub-asset of the prefab.
                surface.navMeshData.name = instance.name + "_NavMeshData";
                AssetDatabase.AddObjectToAsset(surface.navMeshData, prefabPath);

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(prefabPath);

                NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
                int verts = triangulation.vertices != null ? triangulation.vertices.Length : 0;
                Debug.Log(
                    $"[Zombie War] Baked NavMesh into prefab '{prefabPath}'.\n" +
                    $"- Surface collect: Children\n" +
                    $"- Triangulation verts (active): {verts}\n" +
                    $"- navMeshData: {surface.navMeshData.name}");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void clearExistingNavMeshDataAssets(string prefabPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is NavMeshData)
                {
                    Object.DestroyImmediate(assets[i], true);
                }
            }
        }
    }
}
#endif
