using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ZombieWar.Editor
{
    public static class WeaponAddressableSetup
    {
        private const string AugPrefabPath = "Assets/Low Poly Guns/Models/Guns/Prefabs/AUG.prefab";
        private const string FamasPrefabPath = "Assets/Low Poly Guns/Models/Guns/Prefabs/FAMAS.prefab";
        private const string AugPrefabAddress = "ZombieWar/Weapons/AUG";
        private const string FamasPrefabAddress = "ZombieWar/Weapons/FAMAS";

        private const string AugConfigPath = "Assets/_Game/Addressables/Configs/Weapon/Gun_AUG.json";
        private const string FamasConfigPath = "Assets/_Game/Addressables/Configs/Weapon/Gun_FAMAS.json";
        private const string AugConfigAddress = "ZombieWar/Weapons/Configs/AUG";
        private const string FamasConfigAddress = "ZombieWar/Weapons/Configs/FAMAS";

        private const string BulletTracerPath = "Assets/_Game/Prefabs/Vfx/ZombieWar/BulletTracer.prefab";
        private const string MuzzleFlashPath = "Assets/_Game/Prefabs/Vfx/ZombieWar/MuzzleFlashVfx.prefab";
        private const string BulletTracerAddress = "ZombieWar/Vfx/BulletTracer";
        private const string MuzzleFlashAddress = "ZombieWar/Vfx/MuzzleFlash";

        private const string AugFireClipPath = "Assets/PostApocalypseGunsDemo/AssaultRifles/AutoGun_1p_01.wav";
        private const string FamasFireClipPath = "Assets/PostApocalypseGunsDemo/AssaultRifles/AutoGun_1p_02.wav";
        private const string AugFireClipAddress = "ZombieWar/Audio/AutoGun_1p_01";
        private const string FamasFireClipAddress = "ZombieWar/Audio/AutoGun_1p_02";

        [MenuItem("ZombieWar/Addressables/Setup Weapons")]
        public static void SetupWeapons()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("Addressables settings could not be created.");
                return;
            }

            setupAddressableEntry(settings, AugPrefabPath, AugPrefabAddress);
            setupAddressableEntry(settings, FamasPrefabPath, FamasPrefabAddress);
            setupAddressableEntry(settings, AugConfigPath, AugConfigAddress);
            setupAddressableEntry(settings, FamasConfigPath, FamasConfigAddress);
            setupAddressableEntry(settings, BulletTracerPath, BulletTracerAddress);
            setupAddressableEntry(settings, MuzzleFlashPath, MuzzleFlashAddress);
            setupAddressableEntry(settings, AugFireClipPath, AugFireClipAddress);
            setupAddressableEntry(settings, FamasFireClipPath, FamasFireClipAddress);

            deleteLegacyScriptableObjects();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Weapon Addressables configured.\n" +
                $"Prefabs: {AugPrefabAddress}, {FamasPrefabAddress}\n" +
                $"Configs: {AugConfigAddress}, {FamasConfigAddress}\n" +
                $"Vfx: {BulletTracerAddress}, {MuzzleFlashAddress}\n" +
                $"Audio: {AugFireClipAddress}, {FamasFireClipAddress}");
        }

        private static void deleteLegacyScriptableObjects()
        {
            string[] legacyPaths =
            {
                "Assets/_Game/Art/Weapons/Gun_AUG.asset",
                "Assets/_Game/Art/Weapons/Gun_FAMAS.asset",
            };

            for (int i = 0; i < legacyPaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(legacyPaths[i]) != null)
                {
                    AssetDatabase.DeleteAsset(legacyPaths[i]);
                }
            }
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
