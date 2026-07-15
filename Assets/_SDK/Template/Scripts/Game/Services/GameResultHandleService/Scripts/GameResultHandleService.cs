using System;
using System.Collections.Generic;
using UnityEngine;
using Nexzap.Base.Analytics;
using Nexzap.Base;
using Nexzap.Base.Data;
using Nexzap.Base.UI;
using Nexzap.Base.Gameplay;
using Nexzap.Base.UI.Template;

namespace Nexzap.Template
{
    public struct WinPopupData
    {
        public int level;
        public int coinReward;
    }

    public struct LosePopupData
    {
        public int level;
        public FailType failType;
    }

    public struct RevivePopupData
    {
        public int level;
        public FailType failType;
        public string reviveText;     // title
        public string reviveDscText;       // description / sub text
        public long coinPrice;
        public string rewardedPlacement;
        public Sprite reviveSprite;
    }

    public struct GameFailPopupData
    {
        public int level;
        public FailType failType;

        public Sprite loseSprite;
        public string loseText;

        public Sprite reviveSprite;
        public string reviveText;
        public string reviveDscText;
    }

    public class GameResultHandleService : GameplayServiceBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ReviveActionRouter reviveActionRouter;
        [SerializeField] private FailTypeSO failTypeSO;

        private LevelService levelService;
        private GameStateService gameStateService;
        private FeatureUnlockService featureUnlockService;
        private ComboService comboService;

        private bool _busy;

        // cache fail data để revive / lose dùng tiếp
        private bool _hasFailCache;
        private GameFailPopupData _lastFailData;

        public override void OnRegister(GameplayServices services)
        {
            services.TryGet(out levelService);
            services.TryGet(out gameStateService);
            services.TryGet(out featureUnlockService);
            services.TryGet(out comboService);
        }

        public override void OnStart()
        {
            _busy = false;
            _hasFailCache = false;
            _lastFailData = default;

            if (gameStateService != null)
                gameStateService.OnChangeState += OnGameStateChanged;
        }

        public override void OnStop()
        {
            if (gameStateService != null)
                gameStateService.OnChangeState -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState prev, GameState next)
        {
            if (!IsEnabled) return;

            switch (next)
            {
                case GameState.Win:
                    HandleWinState();
                    break;

                case GameState.Lose:
                    HandleLoseState();
                    break;
            }
        }

        private void HandleWinState()
        {
            ShowWin(new WinPopupData
            {
                level = GetLevelFallback(),
                coinReward = GetLevelReward()
            });
        }

        private void HandleLoseState()
        {
            var failType = gameStateService != null
                ? gameStateService.CurrentFailType
                : FailType.TimeUp;

            var cfg = GetFailConfig(failType);

            var failData = new GameFailPopupData
            {
                level = GetLevelFallback(),
                failType = failType,

                loseSprite = cfg != null ? cfg.loseSprite : null,
                loseText = cfg != null ? cfg.loseText : "FAILED",

                reviveSprite = cfg != null ? cfg.reviveSprite : null,
                reviveText = cfg != null ? cfg.reviveTitle : "REVIVE",
                reviveDscText = cfg != null ? cfg.reviveDescription : "Continue?"
            };

            var reviveData = new RevivePopupData
            {
                level = failData.level,
                failType = failType,
                reviveText = failData.reviveText,
                reviveDscText = failData.reviveDscText,
                coinPrice = GetReviveCoinPrice(),
                rewardedPlacement = "revive",
                reviveSprite = failData.reviveSprite,
            };

            ShowFailThenRevive(failData, reviveData, null);
        }

        private FailTypeConfig GetFailConfig(FailType failType)
        {
            if (failTypeSO == null) return null;
            return failTypeSO.GetConfig(failType);
        }

        private int GetLevelFallback()
        {
            if (levelService != null) return levelService.CurrentLevelNumber;
            if (UserProfileController.Instance != null) return UserProfileController.Instance.LEVEL;
            return 1;
        }

        private int GetLevelReward()
        {
            if (ConfigController.Instance != null && ConfigController.Instance.GameConfig != null)
                return ConfigController.Instance.GameConfig.levelReward;
            return 0;
        }

        private long GetReviveCoinPrice()
        {
            if (ConfigController.Instance != null && ConfigController.Instance.GameConfig != null)
                return ConfigController.Instance.GameConfig.levelRevice;
            return 0;
        }

        // =========================================================
        // WIN
        // =========================================================

        private void ShowWin(WinPopupData? overrideData = null, bool force = false)
        {
            if ((!IsEnabled && !force) || (_busy && !force))
            {
                return;
            }
            _busy = true;

            var data = overrideData ?? new WinPopupData
            {
                level = GetLevelFallback(),
                coinReward = GetLevelReward()
            };

            AdvanceLevelForWinIfNeeded(data.level);
            NexzapAnalytics.ReportGameFinished(
                true,
                comboService != null ? comboService.StarTotal : 0f,
                data.level);

            var popup = UIPopupController.Instance.GetActivePopup<UIGameWinPopup>();
            if (popup == null)
            {
                _busy = false;
                return;
            }

            popup.onShowed.RemoveAllListeners();
            popup.onShowed.AddListener(() =>
            {
                if (UserProfileController.Instance != null)
                {
                    int syncLevel;
                    // Rule UX:
                    // - Vừa clear level kế trước mốc unlock (vd clear 4, mốc 5) => fill phải lên 100% của mốc đó.
                    // - Vừa clear đúng mốc unlock (vd clear 5) => chuyển qua mốc feature kế tiếp.
                    if (featureUnlockService != null
                        && featureUnlockService.TryGetFeatureAtLevel(data.level, out _))
                    {
                        syncLevel = UserProfileController.Instance.LEVEL;
                    }
                    else
                    {
                        syncLevel = data.level;
                    }

                    featureUnlockService?.HandleLevelLoaded(syncLevel);
                }
            });

            popup.OnNext = () =>
            {
                _busy = false;
                UISceneController.Instance.ReloadCurrentScene();
            };

            popup.Show();
        }

        private void AdvanceLevelForWinIfNeeded(int completedLevel)
        {
            if (UserProfileController.Instance == null)
            {
                return;
            }

            int safeCompletedLevel = Mathf.Max(1, completedLevel);
            if (UserProfileController.Instance.LEVEL <= safeCompletedLevel)
            {
                UserProfileController.Instance.LEVEL = safeCompletedLevel + 1;
            }
        }

        // =========================================================
        // FAIL -> REVIVE
        // =========================================================

        private void ShowFailThenRevive(
            GameFailPopupData failData,
            RevivePopupData? reviveOverrideData = null,
            Action onReviveSuccess = null)
        {
            if (!IsEnabled || _busy) return;
            _busy = true;

            if (UIPopupController.Instance != null)
                UIPopupController.Instance.HideAllPopups();

            if (failData.level <= 0)
                failData.level = GetLevelFallback();

            _hasFailCache = true;
            _lastFailData = failData;

            var popup = UIPopupController.Instance.GetActivePopup<UIGameFailPopup>();
            if (popup == null)
            {
                _busy = false;
                return;
            }

            popup.SetData(failData);
            popup.SetOnFinished(() =>
            {
                ShowRevive(reviveOverrideData, onReviveSuccess);
            });
            popup.Show();
        }

        private void ShowRevive(RevivePopupData? overrideData, Action onReviveSuccess)
        {
            if (UIPopupController.Instance != null)
                UIPopupController.Instance.HidePopup<UIGameFailPopup>();

            var level = GetLevelFallback();
            var failType = _hasFailCache ? _lastFailData.failType : FailType.TimeUp;

            var reviveTitle = _hasFailCache ? _lastFailData.reviveText : "REVIVE";
            var reviveDescription = _hasFailCache ? _lastFailData.reviveDscText : "Continue?";

            var data = overrideData ?? new RevivePopupData
            {
                level = level,
                failType = failType,
                reviveText = reviveTitle,
                reviveDscText = reviveDescription,
                coinPrice = GetReviveCoinPrice(),
                rewardedPlacement = "revive",
                reviveSprite = _lastFailData.reviveSprite
            };

            if (data.level <= 0) data.level = level;
            data.failType = failType;

            var popup = UIPopupController.Instance.GetActivePopup<UIGameRevivePopup>();
            if (popup == null)
            {
                _busy = false;
                return;
            }

            popup.SetData(data);

            popup.OnGiveUp = () =>
            {
                _busy = false;
                ShowLose(new LosePopupData
                {
                    level = level,
                    failType = failType
                });
            };

            popup.OnRevive = () =>
            {
                _busy = false;

                if (reviveActionRouter != null)
                    reviveActionRouter.TryExecute(failType);

                onReviveSuccess?.Invoke();

                if (gameStateService != null)
                    gameStateService.ResetToPlaying();
            };

            popup.Show();
        }

        // =========================================================
        // LOSE
        // =========================================================

        private void ShowLose(LosePopupData? overrideData = null)
        {
            if (!IsEnabled || _busy) return;
            _busy = true;

            var failTypeFallback = _hasFailCache ? _lastFailData.failType : FailType.TimeUp;

            var data = overrideData ?? new LosePopupData
            {
                level = GetLevelFallback(),
                failType = failTypeFallback
            };

            NexzapAnalytics.ReportGameFinished(
                false,
                comboService != null ? comboService.StarTotal : 0f,
                data.level,
                data.failType.ToString());

            var popup = UIPopupController.Instance.GetActivePopup<UIGameLosePopup>();
            if (popup == null)
            {
                _busy = false;
                return;
            }

            popup.OnRetry = () =>
            {
                _busy = false;
                UISceneController.Instance.ReloadCurrentScene();
            };

            popup.Show();
        }

        // =========================================================
        // OPTIONAL PUBLIC API
        // =========================================================

        public void ForceShowWin()
        {
            _busy = false;
            ShowWin(new WinPopupData
            {
                level = GetLevelFallback(),
                coinReward = GetLevelReward()
            }, true);
        }

        public void ForceShowLose(FailType failType)
        {
            var cfg = GetFailConfig(failType);

            var failData = new GameFailPopupData
            {
                level = GetLevelFallback(),
                failType = failType,

                loseSprite = cfg != null ? cfg.loseSprite : null,
                loseText = cfg != null ? cfg.loseText : "FAILED",

                reviveSprite = cfg != null ? cfg.reviveSprite : null,
                reviveText = cfg != null ? cfg.reviveTitle : "REVIVE",
                reviveDscText = cfg != null ? cfg.reviveDescription : "Continue?",
            };

            var reviveData = new RevivePopupData
            {
                level = failData.level,
                failType = failType,
                reviveText = failData.reviveText,
                reviveDscText = failData.reviveDscText,
                coinPrice = GetReviveCoinPrice(),
                rewardedPlacement = "revive",
                reviveSprite = failData.reviveSprite,
            };

            ShowFailThenRevive(failData, reviveData, null);
        }

        public void ClearFailCache()
        {
            _hasFailCache = false;
            _lastFailData = default;
        }
    }
}
