using System;
using UnityEngine;
using Nexzap.Base.UI;
using Nexzap.Base.Data;

namespace Nexzap.Base.Gameplay
{
    public sealed class FeatureUnlockService : GameplayServiceBehaviour
    {
        [Header("Config")]
        [SerializeField] private FeatureUnlockSO featureSO;

        [Header("Popup")]
        [SerializeField] private bool showPopupOnUnlockLevel = true;

        [Tooltip("Chỉ show mỗi feature đúng 1 lần (save PlayerPrefs).")]
        [SerializeField] private bool showOnlyOnce = true;

        [Header("Debug")]
        [SerializeField] private bool logDebug = false;

        public event Action<FeatureUnlockConfig> OnNextFeatureChanged;

        private const string PrefShownPrefix = "feature_unlock_shown_";

        public override void OnStart()
        {
            // Emit initial next feature (dựa current level)
            int cur = GetCurrentLevelSafe();
            EmitNextFeature(cur);

            HandleLevelLoaded(UserProfileController.Instance.LEVEL);
        }

        public override void OnStop()
        {
        }

        /// <summary>
        /// CALL khi level load xong (vào level).
        /// Nếu bạn muốn auto bind LevelService event, paste LevelService.cs cho mình.
        /// </summary>
        public void HandleLevelLoaded(int currentLevel)
        {
            if (featureSO == null) return;

            if (logDebug)
                Debug.Log($"[FeatureUnlockService] HandleLevelLoaded: level={currentLevel}");

            // 1) show popup unlock
            if (showPopupOnUnlockLevel && featureSO.TryGetConfigByLevel(currentLevel, out var unlocked))
            {
                var popup = UIPopupController.Instance.GetActivePopup<UINewFeaturePopup>();
                if (popup != null)
                {
                    popup.SetData(unlocked);
                    popup.Show();
                }
            }

            // 2) notify next feature
            EmitNextFeature(currentLevel);
        }

        public bool TryGetNextFeature(out FeatureUnlockConfig next)
        {
            next = null;
            if (featureSO == null) return false;

            int cur = GetCurrentLevelSafe();
            return featureSO.TryGetNextConfig(cur, out next);
        }

        public bool TryGetFeatureAtLevel(int level1Based, out FeatureUnlockConfig cfg)
        {
            cfg = null;
            if (featureSO == null) return false;
            return featureSO.TryGetConfigByLevel(level1Based, out cfg);
        }

        /// <summary>
        /// Progress theo đoạn: prevUnlockLevel -> nextUnlockLevel
        /// total  = next - prev
        /// passed = current - prev
        /// </summary>
        public (int passed, int total, float fill01, int prevUnlockLevel, int nextUnlockLevel, FeatureType type) GetNextFeatureProgress()
        {
            int curLevel = GetCurrentLevelSafe();
            if (featureSO == null) return (0, 0, 0f, 0, 0, FeatureType.Booster);

            if (!featureSO.TryGetNextConfig(curLevel, out var next) || next == null)
                return (0, 0, 0f, 0, 0, FeatureType.Booster);

            int prevUnlock = featureSO.GetPrevUnlockLevelOrStart(curLevel, startLevelFallback: 1);
            int nextUnlock = Mathf.Max(prevUnlock + 1, next.unlockAtLevel);

            int total = Mathf.Max(1, nextUnlock - prevUnlock);

            // Nếu bạn muốn "đã qua" tính theo level đã COMPLETE thay vì current level index,
            // bạn có thể thay curLevel bằng (curLevel - 1) ở đây.
            int passed = Mathf.Clamp(curLevel - prevUnlock, 0, total);

            float fill01 = (float)passed / total;

            return (passed, total, fill01, prevUnlock, nextUnlock, next.type);
        }

        private void EmitNextFeature(int level1Based)
        {
            if (featureSO != null && featureSO.TryGetNextConfig(level1Based, out var next))
                OnNextFeatureChanged?.Invoke(next);
            else
                OnNextFeatureChanged?.Invoke(null);
        }

        private bool HasShown(FeatureUnlockConfig cfg)
        {
            if (cfg == null) return true;
            string key = PrefShownPrefix + cfg.type + "_" + cfg.unlockAtLevel;
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        private void MarkShown(FeatureUnlockConfig cfg)
        {
            if (cfg == null) return;
            string key = PrefShownPrefix + cfg.type + "_" + cfg.unlockAtLevel;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        private int GetCurrentLevelSafe()
        {
            return Mathf.Max(1, UserProfileController.Instance.LEVEL);
        }
    }
}