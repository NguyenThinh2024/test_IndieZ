using System;
using Thinh.Base.Analytics;
using Thinh.Template;
using UnityEngine;

namespace Thinh.Base.Gameplay
{
    public enum GameState
    {
        Pause,
        Playing,
        Win,
        Lose
    }

    public sealed class GameStateService : GameplayServiceBehaviour
    {
        public event Action<GameState, GameState> OnChangeState; // (prev, next)

        public GameState State { get; private set; } = GameState.Playing;
        public FailType CurrentFailType { get; private set; } = FailType.TimeUp;

        private GameResultHandleService resultHandleService;
        private ComboService comboService;
        private LevelService levelService;
        private bool hasReportedGameStart;

        public override void OnRegister(GameplayServices services)
        {
            services.TryGet(out resultHandleService);
            services.TryGet(out comboService);
            services.TryGet(out levelService);
        }

        public override void OnStart()
        {
            hasReportedGameStart = false;
            SetState(GameState.Pause);
            OnChangeState += ChangeStateAnalyticsHandler;

            if (State == GameState.Playing)
            {
                ReportGameStarted();
            }
        }

        public override void OnStop()
        {
            OnChangeState -= ChangeStateAnalyticsHandler;
            OnChangeState = null;
        }

        private void ChangeStateAnalyticsHandler(GameState previousState, GameState nextState)
        {
            if (nextState == GameState.Playing && previousState == GameState.Pause)
            {
                ReportGameStarted();
            }

            bool canResultHandlerReport = resultHandleService != null && resultHandleService.IsEnabled;
            if (canResultHandlerReport)
            {
                return;
            }

            if (nextState == GameState.Win)
            {
                ThinhAnalytics.ReportGameFinished(true, ResolveScore(), ResolveLevel());
            }
            else if (nextState == GameState.Lose)
            {
                ThinhAnalytics.ReportGameFinished(false, ResolveScore(), ResolveLevel(), CurrentFailType.ToString());
            }
        }

        private void ReportGameStarted()
        {
            if (hasReportedGameStart)
            {
                return;
            }

            ThinhAnalytics.ReportGameStarted(ResolveLevel());
            hasReportedGameStart = true;
        }

        private int ResolveLevel()
        {
            return levelService != null ? Mathf.Max(1, levelService.CurrentLevelNumber) : 1;
        }

        private float ResolveScore()
        {
            return comboService != null ? comboService.StarTotal : 0f;
        }

        public bool Is(GameState s) => State == s;

        public bool SetState(GameState next)
        {
            if (State == next) return false;

            var prev = State;
            State = next;
            OnChangeState?.Invoke(prev, next);
            return true;
        }

        public bool Play() => SetState(GameState.Playing);

        public bool Pause()
        {
            if (State != GameState.Playing) return false;
            return SetState(GameState.Pause);
        }

        public bool Resume()
        {
            if (State != GameState.Pause) return false;
            return SetState(GameState.Playing);
        }

        public bool Win()
        {
            if (State != GameState.Playing && State != GameState.Pause) return false;
            return SetState(GameState.Win);
        }

        public bool Lose(FailType failType)
        {
            Debug.Log($"Lose {failType}");
            if (State != GameState.Playing && State != GameState.Pause) return false;

            CurrentFailType = failType;
            return SetState(GameState.Lose);
        }

        public void ResetToPlaying()
        {
            SetState(GameState.Playing);
        }
    }
}
