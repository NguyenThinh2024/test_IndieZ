using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ZombieWar.Editor
{
    public static class ZombieEnemyAddressableSetup
    {
        private const string WalkerConfigPath = "Assets/_Game/Addressables/Configs/Enemy/ZombieEnemyConfig.json";
        private const string RunnerConfigPath = "Assets/_Game/Addressables/Configs/Enemy/ZombieRunnerConfig.json";
        private const string ShirtlessBossConfigPath =
            "Assets/_Game/Addressables/Configs/Enemy/ZombieShirtlessBossConfig.json";
        private const string WalkerConfigAddress = "ZombieWar/Enemy/Configs/Zombie";
        private const string RunnerConfigAddress = "ZombieWar/Enemy/Configs/ZombieRunner";
        private const string ShirtlessBossConfigAddress = "ZombieWar/Enemy/Configs/ZombieShirtlessBoss";

        private const string EnemyPrefabPath = "Assets/_Game/Resources/ZombieWar/Zombie/Zombie.prefab";
        private const string EnemyPrefabAddress = "ZombieWar/Enemy/Zombie";
        private const string ShirtlessBossPrefabPath =
            "Assets/_Game/Resources/ZombieWar/Zombie/ShirtlessBoss.prefab";
        private const string ShirtlessBossPrefabAddress = "ZombieWar/Enemy/ShirtlessBoss";

        private const string BaseSkinPath = "Assets/ArtStore3D/Zombie/Materials/Zombie_Mat.mat";
        private const string MaskSkinPath = "Assets/ArtStore3D/Zombie/Texture/Materials/Zombie_MaskMap.mat";
        private const string BaseSkinAddress = "ZombieWar/Enemy/Skins/Base";
        private const string MaskSkinAddress = "ZombieWar/Enemy/Skins/MaskMap";

        private const string MoanPath = "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Moan/zombie_moan_001.wav";
        private const string HissPath = "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Hiss/zombie_hiss_010.wav";
        private const string DeathPath = "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Death/zombie_death_004.wav";
        private const string GruntPath = "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Grunt/zombie_grunt_006.wav";

        private const string MoanAddress = "ZombieWar/Audio/Zombie/Moan001";
        private const string HissAddress = "ZombieWar/Audio/Zombie/Hiss010";
        private const string DeathAddress = "ZombieWar/Audio/Zombie/Death004";
        private const string GruntAddress = "ZombieWar/Audio/Zombie/Grunt006";

        [MenuItem("ZombieWar/Addressables/Setup Zombie Enemy")]
        public static void setupZombieEnemy()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("Addressables settings could not be created.");
                return;
            }

            setupAddressableEntry(settings, WalkerConfigPath, WalkerConfigAddress);
            setupAddressableEntry(settings, RunnerConfigPath, RunnerConfigAddress);
            setupAddressableEntry(settings, ShirtlessBossConfigPath, ShirtlessBossConfigAddress);
            setupAddressableEntry(settings, EnemyPrefabPath, EnemyPrefabAddress);
            setupAddressableEntry(settings, ShirtlessBossPrefabPath, ShirtlessBossPrefabAddress);
            setupAddressableEntry(settings, BaseSkinPath, BaseSkinAddress);
            setupAddressableEntry(settings, MaskSkinPath, MaskSkinAddress);

            setupAddressableEntry(settings, MoanPath, MoanAddress);
            setupAddressableEntry(settings, HissPath, HissAddress);
            setupAddressableEntry(settings, DeathPath, DeathAddress);
            setupAddressableEntry(settings, GruntPath, GruntAddress);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Zombie enemy Addressables configured.\n" +
                $"Configs: {WalkerConfigAddress}, {RunnerConfigAddress}, {ShirtlessBossConfigAddress}\n" +
                $"Prefabs: {EnemyPrefabAddress}, {ShirtlessBossPrefabAddress}\n" +
                $"Skins: {BaseSkinAddress}, {MaskSkinAddress}\n" +
                $"Audio: {MoanAddress}, {HissAddress}, {DeathAddress}, {GruntAddress}");
        }

        private static void setupAddressableEntry(AddressableAssetSettings settings, string assetPath, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"Addressable asset was not found at path: {assetPath}");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.SetAddress(address);
        }
    }
}
