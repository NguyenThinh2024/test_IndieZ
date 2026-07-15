#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ZombieWar.Level;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Each map level uses its prefab (level1/level2) + exactly 3 sequential waves.
    /// Wave N+1 unlocks only after Wave N spawn quota is done and field is clear.
    /// </summary>
    public static class ZombieWarLevelWaveConfigSetup
    {
        private const string MenuPath = "Zombie War/Level/Setup Wave Configs";

        private const string DataFolder = "Assets/_Game/Data/ZombieWar/Level";
        private const string Level1WavePath = DataFolder + "/LevelWaveConfig_Level1.asset";
        private const string Level2WavePath = DataFolder + "/LevelWaveConfig_Level2.asset";
        private const string CatalogPath = DataFolder + "/LevelMapCatalog.asset";

        private const string WalkerGuid = "2c85bd009dff09749ad44311b469c3f1";
        private const string MaskGuid = "1e887034a8e4ef042b2f1683b6a2df2e";
        private const string ShirtlessGuid = "4f3fa7be138594d4badfb6467c42adbc";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            ensureFolder(DataFolder);

            LevelWaveConfig level1 = ensureWaveAsset(Level1WavePath);
            writeLevelWaves(
                level1,
                duration: 180f,
                wave1Guid: WalkerGuid,
                wave1Count: 12,
                wave2Guid: WalkerGuid,
                wave2Count: 16,
                wave3Guid: MaskGuid,
                wave3Count: 18,
                wave3Boss: false);

            LevelWaveConfig level2 = ensureWaveAsset(Level2WavePath);
            writeLevel2HardWaves(level2);

            wireCatalog(level1, level2);

            // Save wave configs only. Catalog SaveAssetIfDirty skipped — Cursor often locks that file.
            AssetDatabase.SaveAssetIfDirty(level1);
            AssetDatabase.SaveAssetIfDirty(level2);

            Debug.Log(
                "[Zombie War] Wave configs ready — 3 sequential waves per map.\n" +
                "- Level 1 prefab → WAVE 1 walker → WAVE 2 walker → WAVE 3 mask\n" +
                "- Level 2 prefab (harder) → WAVE 1 walker → WAVE 2 mask → WAVE 3 Shirtless Elite\n" +
                "Clear a wave fully before the next unlocks.");
        }

        private static void writeLevel2HardWaves(LevelWaveConfig config)
        {
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("durationSeconds").floatValue = 210f;
            SerializedProperty waves = so.FindProperty("waves");
            waves.arraySize = 3;

            writeWave(
                waves.GetArrayElementAtIndex(0),
                startTime: 2.5f,
                interval: 0.55f,
                spawnCount: 20,
                maxAlive: 14,
                displayName: "WAVE 1",
                isBoss: false,
                announceEnabled: true,
                announceLead: 2.5f,
                configGuid: WalkerGuid,
                announceSubtitle: "Elite threat level");

            writeWave(
                waves.GetArrayElementAtIndex(1),
                startTime: 2.5f,
                interval: 0.5f,
                spawnCount: 24,
                maxAlive: 16,
                displayName: "WAVE 2",
                isBoss: false,
                announceEnabled: true,
                announceLead: 2.5f,
                configGuid: MaskGuid,
                announceSubtitle: "Boss wave coming next!");

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
                configGuid: ShirtlessGuid,
                announceSubtitle: "Prepare for the boss!");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void writeLevelWaves(
            LevelWaveConfig config,
            float duration,
            string wave1Guid,
            int wave1Count,
            string wave2Guid,
            int wave2Count,
            string wave3Guid,
            int wave3Count,
            bool wave3Boss)
        {
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("durationSeconds").floatValue = duration;
            SerializedProperty waves = so.FindProperty("waves");
            waves.arraySize = 3;

            // StartTime = delay after wave unlock. Lead = announce before first spawn.
            writeWave(
                waves.GetArrayElementAtIndex(0),
                startTime: 2.5f,
                interval: 0.75f,
                spawnCount: wave1Count,
                maxAlive: 10,
                displayName: "WAVE 1",
                isBoss: false,
                announceEnabled: true,
                announceLead: 2.5f,
                configGuid: wave1Guid);

            writeWave(
                waves.GetArrayElementAtIndex(1),
                startTime: 2.5f,
                interval: 0.7f,
                spawnCount: wave2Count,
                maxAlive: 12,
                displayName: "WAVE 2",
                isBoss: false,
                announceEnabled: true,
                announceLead: 2.5f,
                configGuid: wave2Guid);

            writeWave(
                waves.GetArrayElementAtIndex(2),
                startTime: 2.5f,
                interval: 0.65f,
                spawnCount: wave3Count,
                maxAlive: 12,
                displayName: "WAVE 3",
                isBoss: wave3Boss,
                announceEnabled: true,
                announceLead: 2.5f,
                configGuid: wave3Guid);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
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

        private static void wireCatalog(LevelWaveConfig level1, LevelWaveConfig level2)
        {
            LevelMapCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelMapCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning(
                    $"[Zombie War] LevelMapCatalog not found at {CatalogPath}. " +
                    "Run 'Zombie War/Addressables/Setup Level Maps' first, then re-run this menu.");
                return;
            }

            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty entries = so.FindProperty("entries");
            if (entries.arraySize < 2)
            {
                entries.arraySize = 2;
            }

            SerializedProperty e1 = entries.GetArrayElementAtIndex(0);
            SerializedProperty e2 = entries.GetArrayElementAtIndex(1);
            bool alreadyWired =
                e1.FindPropertyRelative("waveConfig").objectReferenceValue == level1 &&
                e2.FindPropertyRelative("waveConfig").objectReferenceValue == level2 &&
                e1.FindPropertyRelative("mapAddress").stringValue == "ZombieWar/Levels/Level1" &&
                e2.FindPropertyRelative("mapAddress").stringValue == "ZombieWar/Levels/Level2";
            if (alreadyWired)
            {
                return;
            }

            e1.FindPropertyRelative("levelNumber").intValue = 1;
            e1.FindPropertyRelative("mapAddress").stringValue = "ZombieWar/Levels/Level1";
            e1.FindPropertyRelative("displayName").stringValue = "Level 1";
            e1.FindPropertyRelative("waveConfig").objectReferenceValue = level1;

            e2.FindPropertyRelative("levelNumber").intValue = 2;
            e2.FindPropertyRelative("mapAddress").stringValue = "ZombieWar/Levels/Level2";
            e2.FindPropertyRelative("displayName").stringValue = "Level 2";
            e2.FindPropertyRelative("waveConfig").objectReferenceValue = level2;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            try
            {
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Zombie War] Could not save LevelMapCatalog (file locked?): {ex.Message}");
            }
        }

        private static LevelWaveConfig ensureWaveAsset(string path)
        {
            LevelWaveConfig existing = AssetDatabase.LoadAssetAtPath<LevelWaveConfig>(path);
            if (existing != null)
            {
                return existing;
            }

            LevelWaveConfig created = ScriptableObject.CreateInstance<LevelWaveConfig>();
            AssetDatabase.CreateAsset(created, path);
            return created;
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
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
