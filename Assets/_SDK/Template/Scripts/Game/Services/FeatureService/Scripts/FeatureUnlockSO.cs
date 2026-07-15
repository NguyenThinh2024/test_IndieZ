using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexzap.Base.Gameplay
{
    public enum FeatureType
    {
        Booster = 0,
        Obstacle = 1,
        Item = 2
    }

    [Serializable]
    public class FeatureUnlockConfig
    {
        [Header("Identity")]
        public FeatureType type = FeatureType.Booster;

        [Header("Unlock")]
        [Min(1)] public int unlockAtLevel = 1;

        [Header("Content")]
        public Sprite icon;

        [Tooltip("Nội dung mô tả hiển thị trong popup.")]
        public string featureName;
        [TextArea] public string featureDsc = "Describe the feature here...";
    }

    [CreateAssetMenu(menuName = "Nexzap/Feature/Feature Unlock", fileName = "FeatureUnlockSO")]
    public class FeatureUnlockSO : ScriptableObject
    {
        public List<FeatureUnlockConfig> config = new();

        public bool TryGetConfigByLevel(int level1Based, out FeatureUnlockConfig cfg)
        {
            cfg = null;
            if (config == null) return false;

            for (int i = 0; i < config.Count; i++)
            {
                var it = config[i];
                if (it == null) continue;
                if (it.unlockAtLevel == level1Based)
                {
                    cfg = it;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Next = unlockAtLevel nhỏ nhất nhưng >= currentLevel</summary>
        public bool TryGetNextConfig(int curentLevel, out FeatureUnlockConfig next)
        {
            next = null;
            if (config == null || config.Count == 0) return false;

            if (curentLevel > config[config.Count - 1].unlockAtLevel) return false; //vuợt quá level feature cuối => hết feature

            int bestLv = int.MaxValue;
            for (int i = 0; i < config.Count; i++)
            {
                var it = config[i];
                if (it == null) continue;
                if (it.unlockAtLevel < curentLevel) continue;

                if (it.unlockAtLevel < bestLv)
                {
                    bestLv = it.unlockAtLevel;
                    next = it;
                }
            }
            return next != null;
        }

        /// <summary>Prev = unlockAtLevel lớn nhất nhưng < currentLevel</summary>
        public bool TryGetPrevConfig(int currentLevel, out FeatureUnlockConfig prev)
        {
            prev = null;
            if (config == null || config.Count == 0) return false;

            int bestLv = int.MinValue;
            for (int i = 0; i < config.Count; i++)
            {
                var it = config[i];
                if (it == null) continue;
                if (it.unlockAtLevel >= currentLevel) continue;

                if (it.unlockAtLevel > bestLv)
                {
                    bestLv = it.unlockAtLevel;
                    prev = it;
                }
            }
            return prev != null;
        }

        public int GetPrevUnlockLevelOrStart(int currentLevel1Based, int startLevelFallback = 1)
        {
            if (TryGetPrevConfig(currentLevel1Based, out var prev) && prev != null)
                return Mathf.Max(1, prev.unlockAtLevel);

            return Mathf.Max(1, startLevelFallback);
        }

        // ===== UI rule =====
        public static string GetHeaderText(FeatureType type)
        {
            return type switch
            {
                FeatureType.Booster => "New Booster",
                FeatureType.Obstacle => "New Obstacle",
                FeatureType.Item => "New Item",
                _ => "New Feature"
            };
        }

        public static string GetButtonText(FeatureType type)
        {
            return "Continue";
            //return type == FeatureType.Booster ? "Claim" : "Continue";
        }
    }
}