using System;
using Thinh.Base.Data;
using Thinh.Base.Level;
using UnityEngine;

namespace Thinh.Base.Gameplay
{
    /// <summary>
    /// Service version of LevelController (drag-drop, no Singleton).
    /// Keeps the same logic: UserProfileController.Instance.LEVEL is 1-based.
    /// </summary>
    public sealed class LevelService : GameplayServiceBehaviour
    {
        [SerializeField] private LevelConfigSO levelConfig;

        public event Action<int, LevelConfig> OnLevelLoaded; 

        [SerializeField] private int _currentLevelNumber = 1;

        [SerializeField] private LevelConfig _currentLevel;

        /// <summary>1-based level index (Lv.1, Lv.2...)</summary>
        public int CurrentLevelNumber => _currentLevelNumber;

        /// <summary>Current LevelConfig (can be null if config missing/out of range)</summary>
        public LevelConfig CurrentLevel => _currentLevel;

        public override void OnStart()
        {
            _currentLevelNumber = UserProfileController.Instance.LEVEL;
            _currentLevelNumber = Mathf.Max(1, _currentLevelNumber);

            // Load config
            LevelConfig cfg = levelConfig.GetLevelByIndex(_currentLevelNumber - 1);

            _currentLevel = cfg;

            OnLevelLoaded?.Invoke(_currentLevelNumber, _currentLevel);
        }

        /// <summary>Advance to next level.</summary>
        public void NextLevel()
        {
            UserProfileController.Instance.LEVEL += 1;
        }
    }
}
