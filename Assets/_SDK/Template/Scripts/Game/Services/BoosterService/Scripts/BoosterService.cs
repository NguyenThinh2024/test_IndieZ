using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Thinh.Base.Data;
using Thinh.Base.UI;
using Thinh.Base.UI.Template;

namespace Thinh.Base.Gameplay
{
    public class BoosterService : GameplayServiceBehaviour
    {
        // Legacy-ish events (if you already use them)
        public UnityEvent<BoosterType> OnBoosterChanged = new();   // inventory/coin changes affecting booster
        public UnityEvent<BoosterType> OnBoosterUsed = new();      // actually consumed
        public UnityEvent<BoosterType?> OnArmedChanged = new();    // null = none armed

        // For UI binder (cleaner signal)
        public event Action<BoosterType> OnStateChanged;

        [Header("Config")]
        [SerializeField] private BoosterConfigSO configSO;
        private LevelService levelService;

        [Header("Router")]
        [SerializeField] private BoosterInstantRouter instantRouter;
        [SerializeField] private BoosterArmedRouter armedRouter;

        [Header("UI Binder")]
        [SerializeField] private BoosterUIBinder uiBinder;


        [Header("Cooldown")]
        [Tooltip("Cooldown chung sau mỗi click (giây) để chặn spam multi-click.")]
        [SerializeField] private float globalClickCooldown = 0.5f;


        // Runtime state
        private readonly Dictionary<BoosterType, float> _perCdRemaining = new();

        public BoosterType? ArmedBooster { get; private set; }

        private bool _initialized;

        // ---------- State struct for UI ----------
        public struct BoosterState
        {
            public BoosterType type;

            public bool unlocked;
            public int unlockLevel;

            public int amount;
            public int price;

            public Sprite icon;
            public BoosterUseMode mode;

            public bool isArmed;

            public bool inCooldown;
            public float cooldownRemaining;
            public float cooldownTotal;
        }

        public override void OnRegister(GameplayServices services)
        {
            if (levelService == null)
                services.TryGet(out levelService);
        }

        public override void OnStart()
        {
            Initialize();
        }

        public void Initialize(bool force = false)
        {
            if (_initialized && !force) return;

            if (configSO == null)
                Debug.LogWarning("[BoosterService] Missing BoosterConfigSO.");

            ResetSessionState();

            // optional auto bind
            if (uiBinder != null)
                uiBinder.Bind(this);

            _initialized = true;

            Debug.LogWarning("[BoosterService] Initialized.");
        }

        public void SetUIBinder(BoosterUIBinder binder, bool autoBind = true)
        {
            uiBinder = binder;
            if (autoBind && uiBinder != null)
                uiBinder.Bind(this);
        }

        public void ResetSessionState()
        {
            _perCdRemaining.Clear();

            if (ArmedBooster.HasValue)
            {
                ArmedBooster = null;
                OnArmedChanged?.Invoke(null);
            }
        }

        public override void Tick(float dt)
        {
            if (!_initialized) Initialize();

            if (_perCdRemaining.Count == 0) return;

            var keys = _tmpKeys;
            keys.Clear();
            foreach (var kv in _perCdRemaining) keys.Add(kv.Key);

            for (int i = 0; i < keys.Count; i++)
            {
                var t = keys[i];

                float before = _perCdRemaining[t];
                float after = Mathf.Max(0f, before - dt);
                _perCdRemaining[t] = after;

                // ✅ cooldown vừa kết thúc -> refresh UI để button bật lại
                if (before > 0f && after <= 0f)
                {
                    // optional: remove key để dict gọn
                    // _perCdRemaining.Remove(t);  // nếu remove thì cẩn thận vì đang iterate keys list -> OK
                    RaiseStateChanged(t);
                }
            }
        }

        private static readonly List<BoosterType> _tmpKeys = new(16);

        // ---------------- Query ----------------

        public BoosterConfig GetBoosterConfig(BoosterType type)
        {
            if (!IsEnabled || configSO == null) return null;
            return configSO.GetConfig(type);
        }

        public float GetCooldownRemaining(BoosterType type)
        {
            return _perCdRemaining.TryGetValue(type, out var v) ? v : 0f;
        }

        public bool IsInCooldown(BoosterType type)
        {
            return GetCooldownRemaining(type) > 0f;
        }

        public bool IsUnlocked(BoosterConfig cfg)
        {
            int level1 = levelService != null
                ? levelService.CurrentLevelNumber
                : (UserProfileController.Instance != null ? UserProfileController.Instance.LEVEL : 1);

            return level1 >= cfg.unlockLevel;
        }

        public BoosterState GetState(BoosterType type)
        {
            var cfg = GetBoosterConfig(type);

            int amount = UserProfileController.Instance != null
                ? UserProfileController.Instance.GetBoosterAmount(type)
                : 0;

            var st = new BoosterState
            {
                type = type,
                unlocked = cfg != null && IsUnlocked(cfg),
                unlockLevel = cfg != null ? cfg.unlockLevel : 1,
                amount = amount,
                price = cfg != null ? cfg.price : 0,
                icon = cfg != null ? cfg.icon : null,
                mode = cfg != null ? cfg.useMode : BoosterUseMode.Instant,
                isArmed = ArmedBooster.HasValue && ArmedBooster.Value == type,
                cooldownRemaining = GetCooldownRemaining(type),
                cooldownTotal = cfg != null ? cfg.cooldownSeconds : 0f
            };

            st.inCooldown = IsInCooldown(type);
            return st;
        }

        // ---------------- Commands ----------------

        public void ClickBooster(BoosterType type)
        {
            if (!IsEnabled) return;
            if (!_initialized) Initialize();

            if (IsInCooldown(type)) return;

            var cfg = GetBoosterConfig(type);
            if (cfg == null) return;
            if (!IsUnlocked(cfg)) return;

            // apply cooldown immediately (anti double-click)
            if (cfg.cooldownSeconds > 0f)
                _perCdRemaining[type] = cfg.cooldownSeconds;
            else _perCdRemaining[type] = globalClickCooldown; //block spam click

            if (cfg.useMode == BoosterUseMode.Instant)
            {
                // 0) apply gameplay via router
                if (instantRouter != null && !instantRouter.TryApply(type))
                    return;

                // 1) then consume
                if (TryConsumeInstantBooster(type, cfg))
                {
                    OnBoosterUsed?.Invoke(type);
                    RaiseStateChanged(type);
                }
                return;
            }
            else
            {
                if (TryConsumeArmedBooster(type, cfg))
                {
                    // Interactive: toggle arm/cancel 
                    ToggleArm(type);
                }
            }
        }

        public bool TryApplyArmedOnTarget(Component target)
        {
            if (!ArmedBooster.HasValue) return false;
            if (armedRouter == null) return false;

            return TryApplyArmedBooster((type) => armedRouter.TryApply(type, target));
        }

        private void ToggleArm(BoosterType type)
        {
            // cancel same
            if (ArmedBooster.HasValue && ArmedBooster.Value == type)
            {
                ArmedBooster = null;
                OnArmedChanged?.Invoke(null);

                RaiseStateChanged(type);
                return;
            }

            // switch armed
            var prev = ArmedBooster;
            ArmedBooster = type;
            OnArmedChanged?.Invoke(ArmedBooster);

            if (prev.HasValue) RaiseStateChanged(prev.Value);
            RaiseStateChanged(type);
        }

        public void CancelArmed()
        {
            if (!ArmedBooster.HasValue) return;

            var prev = ArmedBooster.Value;
            ArmedBooster = null;
            OnArmedChanged?.Invoke(null);

            RaiseStateChanged(prev);
        }

        /// <summary>
        /// Gameplay calls this when player clicks something while a booster is armed.
        /// applyAction returns true if gameplay actually applied the booster effect.
        /// Only then we consume booster/coin.
        /// </summary>
        public bool TryApplyArmedBooster(Func<BoosterType, bool> applyAction)
        {
            if (!IsEnabled) return false;
            if (!_initialized) Initialize();

            if (!ArmedBooster.HasValue) return false;

            var type = ArmedBooster.Value;
            var cfg = GetBoosterConfig(type);
            if (cfg == null) return false;

            bool applied = applyAction != null && applyAction(type);
            if (!applied) return false;

            if (!TryConsumeInstantBooster(type, cfg))
                return false;

            ArmedBooster = null;
            OnArmedChanged?.Invoke(null);

            OnBoosterUsed?.Invoke(type);
            RaiseStateChanged(type);

            return true;
        }

        // ---------------- Economy ----------------

        private bool TryConsumeInstantBooster(BoosterType boosterType, BoosterConfig cfg)
        {
            var profile = UserProfileController.Instance;
            if (profile == null) return false;

            // 1) consume inventory
            if (profile.UseBooster(boosterType))
                return true;

            // 2) buy by coin
            if (profile.UseCoin(cfg.price))
                return true;

            // 3) not enough coin
            UIPopupController.Instance?.GetActivePopup<UIPopupGetCoins>();
            return false;
        }

        private bool TryConsumeArmedBooster(BoosterType boosterType, BoosterConfig cfg)
        {
            var profile = UserProfileController.Instance;
            if (profile == null) return false;

            // 1) consume inventory
            if (profile.GetBoosterAmount(boosterType) > 0)
                return true;

            // 2) buy by coin
            if (profile.userData.coin > cfg.price)
                return true;

            // 3) not enough coin
            UIPopupController.Instance?.GetActivePopup<UIPopupGetCoins>();
            return false;
        }

        // ---------------- Notify UI ----------------

        /// <summary>
        /// Call this when any external system changes booster amount/coin and UI must update.
        /// </summary>
        public void RefreshBooster(BoosterType type)
        {
            RaiseStateChanged(type);
        }

        private void RaiseStateChanged(BoosterType type)
        {
            // legacy event
            OnBoosterChanged?.Invoke(type);

            // clean event
            OnStateChanged?.Invoke(type);

            // push UI binder directly (no need to subscribe, but both are ok)
            if (uiBinder != null)
                uiBinder.Refresh(type);
        }
    }
}