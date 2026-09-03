using System;
using Thinh.Base.Data;
using Thinh.Template;
using UnityEngine;

namespace Thinh.Base.Gameplay
{
    public sealed class TimerService : GameplayServiceBehaviour
    {
        [Header("Service")]
        [SerializeField] private bool enableCountdown = false;

        [Header("UI Auto Bind")]
        [SerializeField] private bool autoBindUI = true;
        [SerializeField] private UITimer timerUI;

        [Tooltip("Fallback nếu không lấy được time từ LevelConfig")]
        [SerializeField] private float fallbackDurationSeconds = 90f;

        [Header("Timer Options")]
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Low Time Warning")]
        [SerializeField] private float lowTimeWarningSeconds = 15f; // dưới mốc này sẽ cảnh báo mỗi giây

        public event Action<float, float> OnChanged; // (remaining, duration)
        public event Action OnTimeUp;
        public event Action<int> OnLowTimeTick;      // bắn mỗi giây khi còn <= lowTimeWarningSeconds

        public float Duration { get; private set; }
        public float Remaining { get; private set; }

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        private bool _timeUpFired;
        private int _lastLowTimeSecond = -1;

        private LevelService _levelService;
        private GameStateService _gameStateService;

        public override void OnStart()
        {
            IsRunning = false;
            IsPaused = false;
            _timeUpFired = false;
            _lastLowTimeSecond = -1;

            if (autoBindUI) AutoBindUI();

            BindServices();

            float t = GetDurationFromLevelOrFallback();

            bool canRun = IsPlayingState();
            StartTimer(t, startImmediately: enableCountdown && canRun);
        }

        private void OnDisable()
        {
            UnbindServices();
        }

        private void BindServices()
        {
            GameController.Instance.Services.TryGet(out _gameStateService);
            if (_gameStateService != null)
                _gameStateService.OnChangeState += HandleStateChanged;
        }

        private void UnbindServices()
        {
            if (_levelService != null)
            {
                _levelService = null;
            }

            if (_gameStateService != null)
            {
                _gameStateService.OnChangeState -= HandleStateChanged;
                _gameStateService = null;
            }
        }

        private bool IsPlayingState()
        {
            return _gameStateService == null || _gameStateService.State == GameState.Playing;
        }

        private void HandleStateChanged(GameState prev, GameState next)
        {
            switch (next)
            {
                case GameState.Playing:
                    if (enableCountdown && Remaining > 0f)
                    {
                        IsRunning = true;
                        IsPaused = false;
                        OnChanged?.Invoke(Remaining, Duration);
                    }
                    break;

                case GameState.Pause:
                    PauseTimer();
                    break;

                case GameState.Win:
                case GameState.Lose:
                    StopTimer();
                    break;
            }
        }

        public void StartTimer(float durationSeconds, bool startImmediately = true)
        {
            Duration = Mathf.Max(0f, durationSeconds);
            Remaining = Duration;

            IsPaused = false;
            _timeUpFired = false;
            _lastLowTimeSecond = -1;

            bool canRun = enableCountdown && startImmediately && Duration > 0f && IsPlayingState();
            IsRunning = canRun;

            OnChanged?.Invoke(Remaining, Duration);
        }

        public void ResetFromLevel(bool startImmediately = true)
        {
            float t = GetDurationFromLevelOrFallback();
            StartTimer(t, startImmediately && IsPlayingState());
        }

        public void StopTimer()
        {
            IsRunning = false;
            IsPaused = false;
        }

        public void PauseTimer()
        {
            if (!IsRunning) return;
            IsPaused = true;
        }

        public void ResumeTimer()
        {
            if (!enableCountdown) return;
            if (!IsPlayingState()) return;
            if (Remaining <= 0f) return;

            IsRunning = true;
            IsPaused = false;
        }

        public void AddTime(float seconds)
        {
            Remaining = Mathf.Max(0f, Remaining + seconds);

            if (Remaining > lowTimeWarningSeconds)
                _lastLowTimeSecond = -1;

            if (Remaining > 0f)
            {
                _timeUpFired = false;
                IsPaused = false;
                IsRunning = enableCountdown && IsPlayingState();
            }

            OnChanged?.Invoke(Remaining, Duration);
        }

        public override void Tick(float dt)
        {
            if (!enableCountdown) return;
            if (!IsPlayingState()) return;
            if (!IsRunning) return;
            if (IsPaused) return;

            float delta = useUnscaledTime ? Time.unscaledDeltaTime : dt;

            if (Remaining <= 0f)
            {
                FireTimeUpOnce();
                return;
            }

            Remaining -= delta;

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                OnChanged?.Invoke(Remaining, Duration);
                FireTimeUpOnce();
                return;
            }

            FireLowTimeTickIfNeeded();
            OnChanged?.Invoke(Remaining, Duration);
        }

        private void FireLowTimeTickIfNeeded()
        {
            if (Remaining > lowTimeWarningSeconds) return;

            int second = Mathf.CeilToInt(Remaining);
            if (second <= 0) return;

            if (second == _lastLowTimeSecond) return;

            _lastLowTimeSecond = second;
            OnLowTimeTick?.Invoke(second);
        }

        private void FireTimeUpOnce()
        {
            if (_timeUpFired) return;

            _timeUpFired = true;
            IsRunning = false;
            IsPaused = false;

            OnTimeUp?.Invoke();

            if (_gameStateService != null)
            {
                _gameStateService.Lose(FailType.TimeUp);
            }
            else
            {
                Debug.LogWarning("[TimerService] TimeUp nhưng chưa gán GameStateService.");
            }
        }

        private float GetDurationFromLevelOrFallback()
        {
            if (_levelService == null) return fallbackDurationSeconds;

            var level = _levelService.CurrentLevel;
            if (level == null) return fallbackDurationSeconds;

            float seconds = level.second;
            return seconds > 0f ? seconds : fallbackDurationSeconds;
        }

        private void AutoBindUI()
        {
#if UNITY_2022_2_OR_NEWER
            if (timerUI == null)
                timerUI = UnityEngine.Object.FindFirstObjectByType<UITimer>(FindObjectsInactive.Include);
#else
            if (timerUI == null)
                timerUI = UnityEngine.Object.FindObjectOfType<UITimer>(true);
#endif

            if (timerUI != null)
                timerUI.Bind(this);
            else
                Debug.LogWarning("[TimerService] autoBindUI bật nhưng không tìm thấy UITimer trong scene.");
        }
    }
}