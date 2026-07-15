using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ZombieWar.Editor
{
    public static class PlayerCharacterAddressableSetup
    {
        private const string CharacterConfigPath = "Assets/_Game/Addressables/Configs/Player/SoldierCharacterConfig.json";
        private const string CharacterConfigAddress = "ZombieWar/Player/Configs/Soldier";
        private const string CharacterPrefabPath = "Assets/_Game/Resources/ZombieWar/Player/PlayerArmature.prefab";
        private const string CharacterPrefabAddress = "ZombieWar/Player/PlayerArmature";
        private const string AnimatorControllerPath = "Assets/_Game/Art/Animations/Player/PlayerCombat.controller";
        private const string AnimatorControllerAddress = "ZombieWar/Player/Anim/PlayerCombat";

        [MenuItem("ZombieWar/Addressables/Setup Player Character")]
        public static void setupPlayerCharacter()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("Addressables settings could not be created.");
                return;
            }

            setupAddressableEntry(settings, CharacterConfigPath, CharacterConfigAddress);
            setupAddressableEntry(settings, CharacterPrefabPath, CharacterPrefabAddress);
            if (AssetDatabase.LoadAssetAtPath<Object>(AnimatorControllerPath) != null)
            {
                setupAddressableEntry(settings, AnimatorControllerPath, AnimatorControllerAddress);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"Player character Addressables configured. Prefab: {CharacterPrefabAddress}, Anim: {AnimatorControllerAddress}");
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
