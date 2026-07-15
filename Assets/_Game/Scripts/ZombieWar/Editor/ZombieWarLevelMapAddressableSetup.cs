#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieWar.Level;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Moves large map prefabs out of Resources, registers Addressables,
    /// creates LevelMapCatalog, and wires LevelMapBootstrap in Gameplay.
    /// </summary>
    public static class ZombieWarLevelMapAddressableSetup
    {
        private const string MenuPath = "Zombie War/Addressables/Setup Level Maps";

        private const string ResourcesLevel1 = "Assets/_Game/Resources/ZombieWar/PrefabsLevel/level1.prefab";
        private const string ResourcesLevel2 = "Assets/_Game/Resources/ZombieWar/PrefabsLevel/level2.prefab";

        private const string LevelsFolder = "Assets/_Game/Prefabs/ZombieWar/Levels";
        private const string Level1Path = LevelsFolder + "/level1.prefab";
        private const string Level2Path = LevelsFolder + "/level2.prefab";

        private const string Level1Address = "ZombieWar/Levels/Level1";
        private const string Level2Address = "ZombieWar/Levels/Level2";

        private const string CatalogFolder = "Assets/_Game/Data/ZombieWar/Level";
        private const string CatalogPath = CatalogFolder + "/LevelMapCatalog.asset";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            Setup(skipSceneWire: false);
        }

        public static void Setup(bool skipSceneWire)
        {
            ensureFolder("Assets/_Game/Prefabs");
            ensureFolder("Assets/_Game/Prefabs/ZombieWar");
            ensureFolder(LevelsFolder);
            ensureFolder("Assets/_Game/Data");
            ensureFolder("Assets/_Game/Data/ZombieWar");
            ensureFolder(CatalogFolder);

            moveIfNeeded(ResourcesLevel1, Level1Path);
            moveIfNeeded(ResourcesLevel2, Level2Path);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[Zombie War] Addressables settings could not be created.");
                return;
            }

            setupAddressableEntry(settings, Level1Path, Level1Address);
            setupAddressableEntry(settings, Level2Path, Level2Address);
            EditorUtility.SetDirty(settings);

            LevelMapCatalog catalog = ensureCatalog();
            if (!skipSceneWire && catalog != null)
            {
                wireGameplayScene(catalog);
            }

            // Do not SaveAssetIfDirty(catalog) — Cursor/IDE often locks LevelMapCatalog.asset.
            if (settings != null)
            {
                AssetDatabase.SaveAssetIfDirty(settings);
            }
            Debug.Log(
                "[Zombie War] Level maps ready for Addressables.\n" +
                $"- {Level1Address} → {Level1Path}\n" +
                $"- {Level2Address} → {Level2Path}\n" +
                $"- Catalog: {CatalogPath}\n" +
                "- Scene: LevelMapBootstrap loads one map; next map is preloaded after ready.\n" +
                "- Baked scene 'level1' instance is removed if present.");
        }

        private static void moveIfNeeded(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath) != null)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
            {
                Debug.LogWarning($"[Zombie War] Level prefab missing at {sourcePath}");
                return;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[Zombie War] Move failed: {sourcePath} → {destinationPath}\n{error}");
            }
            else
            {
                Debug.Log($"[Zombie War] Moved {sourcePath} → {destinationPath}");
            }
        }

        private static LevelMapCatalog ensureCatalog()
        {
            LevelMapCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelMapCatalog>(CatalogPath);
            if (catalog == null && System.IO.File.Exists(CatalogPath))
            {
                // File exists but Unity couldn't import (often file lock). Never CreateAsset over it.
                AssetDatabase.ImportAsset(CatalogPath, ImportAssetOptions.ForceSynchronousImport);
                catalog = AssetDatabase.LoadAssetAtPath<LevelMapCatalog>(CatalogPath);
            }

            if (catalog == null && System.IO.File.Exists(CatalogPath))
            {
                Debug.LogError(
                    $"[Zombie War] {CatalogPath} exists but is locked/unreadable. " +
                    "Close tabs that have this asset open in Cursor/IDE, then Reimport — refusing CreateAsset overwrite.");
                return null;
            }

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelMapCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                SerializedObject createdSo = new SerializedObject(catalog);
                SerializedProperty createdEntries = createdSo.FindProperty("entries");
                createdEntries.arraySize = 2;
                writeEntry(createdEntries.GetArrayElementAtIndex(0), 1, Level1Address, "Level 1");
                writeEntry(createdEntries.GetArrayElementAtIndex(1), 2, Level2Address, "Level 2");
                createdSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                return catalog;
            }

            // Existing catalog on disk: do NOT SetDirty / rewrite.
            // Idle Cursor/IDE locks were causing Unity TempFile move Access denied.
            return catalog;
        }

        private static void writeEntry(SerializedProperty entry, int levelNumber, string address, string displayName)
        {
            entry.FindPropertyRelative("levelNumber").intValue = levelNumber;
            entry.FindPropertyRelative("mapAddress").stringValue = address;
            entry.FindPropertyRelative("displayName").stringValue = displayName;
        }

        private static void wireGameplayScene(LevelMapCatalog catalog)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[Zombie War] No active scene to wire LevelMapBootstrap.");
                return;
            }

            removeBakedLevelInstance();

            GameObject root = GameObject.Find("ZW_LevelMapRoot");
            if (root == null)
            {
                root = new GameObject("ZW_LevelMapRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create Level Map Root");
            }

            LevelMapBootstrap bootstrap = root.GetComponent<LevelMapBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = Undo.AddComponent<LevelMapBootstrap>(root);
            }

            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("catalog").objectReferenceValue = catalog;
            bootstrapSo.FindProperty("levelNumber").intValue = 1;
            bootstrapSo.FindProperty("mapParent").objectReferenceValue = root.transform;
            bootstrapSo.FindProperty("loadOnEnable").boolValue = true;
            bootstrapSo.FindProperty("preloadNextLevel").boolValue = true;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            ZombieWarGameFlow gameFlow = UnityEngine.Object.FindFirstObjectByType<ZombieWarGameFlow>();
            if (gameFlow != null)
            {
                SerializedObject flowSo = new SerializedObject(gameFlow);
                flowSo.FindProperty("levelMapBootstrap").objectReferenceValue = bootstrap;
                flowSo.FindProperty("waitForMapBeforeStart").boolValue = true;
                flowSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void removeBakedLevelInstance()
        {
            // Maps must come from Addressables only — clear baked scene instance under ZW_LevelMapRoot.
            GameObject root = GameObject.Find("ZW_LevelMapRoot");
            if (root != null)
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = root.transform.GetChild(i);
                    string childName = child.name;
                    Undo.DestroyObjectImmediate(child.gameObject);
                    Debug.Log($"[Zombie War] Removed baked map child '{childName}' (will load via Addressables).");
                }
            }

            GameObject baked = GameObject.Find("level1") ?? GameObject.Find("level2");
            if (baked == null)
            {
                return;
            }

            // Also remove orphan scene-root baked maps.
            if (baked.transform.parent != null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(baked);
            Debug.Log("[Zombie War] Removed baked scene map instance (will load via Addressables).");
        }

        private static void setupAddressableEntry(AddressableAssetSettings settings, string assetPath, string address)
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

        private static void ensureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            ensureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
