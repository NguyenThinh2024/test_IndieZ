using System;
using Nexzap.Base.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

public class OnboardingService : GameplayServiceBehaviour
{
    [Header("Config")]
    [SerializeField] private OnboardingConfigSO config;
    [FormerlySerializedAs("autoPlayFirstOnLevelLoadedStep")]
    [SerializeField] private bool autoPlayFirstStep = true;
    [SerializeField] private bool tryBootstrapFromCurrentRuntime = true;
    [SerializeField] private bool verboseLogging;

    [Header("Runtime")]
    [FormerlySerializedAs("adapterSource")]
    [FormerlySerializedAs("targetResolverSource")]
    [FormerlySerializedAs("runtimeBridgeSource")]
    [SerializeField] private MonoBehaviour targetAdapterSource;
    [SerializeField] private MonoBehaviour sceneManagerSource;

    [Header("Presentation")]
    [SerializeField] private TutorialStateTargetMask tutorialMask;
    [SerializeField] private PanelTap panelTap;

    private LevelService _levelService;
    private ILevelRuntime _sceneManager;
    private IOnboardingAdapter _adapter;
    private bool _isSubscribedToSceneManager;
    private bool _isSubscribedToAdapter;
    private bool _awaitingRuntimeBootstrap;
    private int _pendingLevelIndex = -1;
    private int _scheduledStepIndex = -1;
    private float _scheduledStepDelayRemaining;

    private OnboardingConfigSO.LevelOnboarding _activeLevel;
    private OnboardingResolvedTarget _currentResolvedTarget;
    private OnboardingConfigSO.StepData _currentStep;
    private int _currentStepIndex = -1;

    public override void OnRegister(GameplayServices services)
    {
        _levelService = services.Get<LevelService>();
    }

    public override void OnStart()
    {
        OnboardingRuntimeSettings.EnabledChanged += HandleOnboardingEnabledChanged;

        if (_levelService != null)
        {
            _levelService.OnLevelLoaded += HandleServiceLevelLoaded;
        }

        if (panelTap != null)
        {
            panelTap.ActionButtonClicked += HandlePanelActionButtonClicked;
        }

        TryResolveSceneManager(forceResubscribe: true);
        TryResolveAdapter(forceResubscribe: true);
        ResolveTutorialMaskIfNeeded();
        ClearPresentation();
        ClearActiveState();

        if (!OnboardingRuntimeSettings.IsEnabled)
        {
            Log("Onboarding is disabled via cheat toggle.");
            return;
        }

        if (tryBootstrapFromCurrentRuntime)
        {
            int currentLevelIndex = GetCurrentKnownLevelIndex();
            if (currentLevelIndex >= 0)
            {
                _pendingLevelIndex = currentLevelIndex;
                _awaitingRuntimeBootstrap = true;
            }

            TryActivatePendingFlow();
        }
    }

    public override void Tick(float dt)
    {
        if (!OnboardingRuntimeSettings.IsEnabled)
        {
            return;
        }

        if (_sceneManager == null)
        {
            TryResolveSceneManager(forceResubscribe: true);
        }

        if (_adapter == null)
        {
            TryResolveAdapter(forceResubscribe: true);
        }

        if (_awaitingRuntimeBootstrap)
        {
            TryActivatePendingFlow();
        }

        TickScheduledStep(dt);
    }

    public override void OnStop()
    {
        OnboardingRuntimeSettings.EnabledChanged -= HandleOnboardingEnabledChanged;

        if (_levelService != null)
        {
            _levelService.OnLevelLoaded -= HandleServiceLevelLoaded;
        }

        if (panelTap != null)
        {
            panelTap.ActionButtonClicked -= HandlePanelActionButtonClicked;
        }

        SubscribeToSceneManager(false);
        SubscribeToAdapter(false);
        ClearPresentation();
        ClearActiveState();
    }

    private void HandleServiceLevelLoaded(int levelNumber, Nexzap.Base.Level.LevelConfig _)
    {
        _pendingLevelIndex = Mathf.Max(0, levelNumber - 1);
        _awaitingRuntimeBootstrap = true;
        Log($"LevelService loaded level {_pendingLevelIndex}.");
    }

    private void HandleGameplayLevelLoaded()
    {
        if (_sceneManager == null)
        {
            return;
        }

        _pendingLevelIndex = Mathf.Max(0, _sceneManager.GetCurrentLevelIndexPublic());
        _awaitingRuntimeBootstrap = true;
        Log($"Gameplay level loaded event received for level {_pendingLevelIndex}.");
        TryActivatePendingFlow();
    }

    private void HandleRuntimeTapProgressed()
    {
        if (_activeLevel == null || _currentStep == null)
        {
            return;
        }

        if (_currentStep.Advance != OnboardingConfigSO.AdvanceMode.TapResolvedTarget)
        {
            return;
        }

        Log($"Runtime tap completed for step '{_currentStep.StepId}'. Advancing to next step.");
        NextStep();
    }

    private void TryResolveSceneManager(bool forceResubscribe)
    {
        ILevelRuntime resolved = sceneManagerSource as ILevelRuntime;
        if (resolved == null)
        {
            Log("No gameplay scene manager source assigned.");
            return;
        }

        if (ReferenceEquals(_sceneManager, resolved) && !forceResubscribe)
        {
            return;
        }

        SubscribeToSceneManager(false);
        _sceneManager = resolved;
        SubscribeToSceneManager(true);
        Log($"Resolved scene manager: {_sceneManager.GetType().Name}.");
    }

    private void SubscribeToSceneManager(bool subscribe)
    {
        if (_sceneManager == null)
        {
            _isSubscribedToSceneManager = false;
            return;
        }

        if (subscribe)
        {
            if (_isSubscribedToSceneManager)
            {
                return;
            }

            _sceneManager.LevelLoaded += HandleGameplayLevelLoaded;
            _isSubscribedToSceneManager = true;
            return;
        }

        if (!_isSubscribedToSceneManager)
        {
            return;
        }

        _sceneManager.LevelLoaded -= HandleGameplayLevelLoaded;
        _isSubscribedToSceneManager = false;
    }

    private void TryResolveAdapter(bool forceResubscribe)
    {
        MonoBehaviour source = targetAdapterSource;
        if (source == null)
        {
            source = FindAdapterSource();
            targetAdapterSource = source;
        }

        if (source == null)
        {
            Log("No onboarding adapter found.");
            return;
        }

        if (!(source is IOnboardingAdapter resolved))
        {
            Log($"Adapter '{source.GetType().Name}' does not implement {nameof(IOnboardingAdapter)}.");
            return;
        }

        if (ReferenceEquals(_adapter, resolved) && !forceResubscribe)
        {
            return;
        }

        SubscribeToAdapter(false);
        _adapter = resolved;
        SubscribeToAdapter(true);
        Log($"Resolved onboarding adapter: {source.GetType().Name}.");
    }

    private MonoBehaviour FindAdapterSource()
    {
        MonoBehaviour[] localBehaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < localBehaviours.Length; i++)
        {
            if (localBehaviours[i] is IOnboardingAdapter)
            {
                return localBehaviours[i];
            }
        }

        return null;
    }

    private void SubscribeToAdapter(bool subscribe)
    {
        if (_adapter == null)
        {
            _isSubscribedToAdapter = false;
            return;
        }

        if (subscribe)
        {
            if (_isSubscribedToAdapter)
            {
                return;
            }

            _adapter.TapProgressed += HandleRuntimeTapProgressed;
            _isSubscribedToAdapter = true;
            return;
        }

        if (!_isSubscribedToAdapter)
        {
            return;
        }

        _adapter.TapProgressed -= HandleRuntimeTapProgressed;
        _isSubscribedToAdapter = false;
    }

    private void TryActivatePendingFlow()
    {
        if (!_awaitingRuntimeBootstrap)
        {
            return;
        }

        if (!OnboardingRuntimeSettings.IsEnabled)
        {
            return;
        }

        if (_sceneManager == null || _adapter == null || !_adapter.IsReady)
        {
            return;
        }

        int runtimeLevelIndex = Mathf.Max(0, _sceneManager.GetCurrentLevelIndexPublic());
        int effectiveLevelIndex = _pendingLevelIndex >= 0 ? _pendingLevelIndex : runtimeLevelIndex;
        if (effectiveLevelIndex != runtimeLevelIndex)
        {
            Log($"Waiting: LevelService pending level {effectiveLevelIndex} but runtime reports {runtimeLevelIndex}.");
            effectiveLevelIndex = runtimeLevelIndex;
        }

        _awaitingRuntimeBootstrap = false;
        ActivateLevel(effectiveLevelIndex);
    }

    private void HandleOnboardingEnabledChanged(bool isEnabled)
    {
        if (!isEnabled)
        {
            Log("Onboarding disabled via cheat toggle.");
            ClearPresentation();
            ClearActiveState();
            return;
        }

        int currentLevelIndex = GetCurrentKnownLevelIndex();
        if (currentLevelIndex < 0)
        {
            Log("Onboarding enabled via cheat toggle, but current level is not available yet.");
            return;
        }

        _pendingLevelIndex = currentLevelIndex;
        _awaitingRuntimeBootstrap = true;
        Log($"Onboarding enabled via cheat toggle. Rebootstrapping level {currentLevelIndex}.");
        TryActivatePendingFlow();
    }

    private int GetCurrentKnownLevelIndex()
    {
        if (_sceneManager != null)
        {
            return Mathf.Max(0, _sceneManager.GetCurrentLevelIndexPublic());
        }

        if (_levelService != null && _levelService.CurrentLevel != null)
        {
            return Mathf.Max(0, _levelService.CurrentLevelNumber - 1);
        }

        return -1;
    }

    private void ActivateLevel(int levelIndex)
    {
        if (config == null || !config.TryGetLevel(levelIndex, out OnboardingConfigSO.LevelOnboarding level))
        {
            Log($"No onboarding level bound for level {levelIndex}. Clearing active onboarding state.");
            ClearPresentation();
            ClearActiveState();
            return;
        }

        ResetStepState();
        _activeLevel = level;
        Log($"Activated onboarding level {levelIndex}.");

        if (!autoPlayFirstStep)
        {
            return;
        }

        ScheduleFirstStep();
    }

    private void ScheduleFirstStep()
    {
        int stepIndex = FindNextValidStepIndex(-1);
        if (stepIndex < 0)
        {
            Log("Active onboarding level does not contain any valid step.");
            ClearPresentation();
            ResetStepRuntimeState();
            return;
        }

        ScheduleStep(stepIndex);
    }

    private int FindNextValidStepIndex(int fromIndexExclusive)
    {
        if (_activeLevel == null || _activeLevel.StepCount <= 0)
        {
            return -1;
        }

        for (int i = fromIndexExclusive + 1; i < _activeLevel.StepCount; i++)
        {
            if (_activeLevel.TryGetStep(i, out _))
            {
                return i;
            }
        }

        return -1;
    }

    private void ScheduleStep(int stepIndex)
    {
        if (_activeLevel == null || !_activeLevel.TryGetStep(stepIndex, out OnboardingConfigSO.StepData step))
        {
            CompleteFlow();
            return;
        }

        float delaySeconds = step.DelayBeforeShowing;
        if (delaySeconds <= 0f)
        {
            ShowStep(stepIndex);
            return;
        }

        _scheduledStepIndex = stepIndex;
        _scheduledStepDelayRemaining = delaySeconds;
        ClearPresentation();
        ResetStepRuntimeState();
        Log($"Scheduled step {stepIndex} with delay {delaySeconds:0.##}s.");
    }

    private void TickScheduledStep(float dt)
    {
        if (_scheduledStepIndex < 0)
        {
            return;
        }

        _scheduledStepDelayRemaining -= Mathf.Max(0f, dt);
        if (_scheduledStepDelayRemaining > 0f)
        {
            return;
        }

        int stepIndex = _scheduledStepIndex;
        CancelScheduledStep();
        ShowStep(stepIndex);
    }

    private void CancelScheduledStep()
    {
        _scheduledStepIndex = -1;
        _scheduledStepDelayRemaining = 0f;
    }

    private void ShowStep(int stepIndex)
    {
        CancelScheduledStep();

        if (_activeLevel == null)
        {
            ClearPresentation();
            ResetStepRuntimeState();
            return;
        }

        if (!_activeLevel.TryGetStep(stepIndex, out OnboardingConfigSO.StepData step))
        {
            CompleteFlow();
            return;
        }

        _currentStep = step;
        _currentStepIndex = stepIndex;
        _currentResolvedTarget = default;

        ResolveTutorialMaskIfNeeded();
        ShowPanel(step);

        if (step.Focus == OnboardingConfigSO.FocusMode.None)
        {
            HideMask();
            _adapter?.ClearStepAction();
            Log($"Step '{step.StepId}' has no focus target.");
            return;
        }

        if (_adapter == null)
        {
            HideMask();
            Log($"Step '{step.StepId}' could not be shown because onboarding adapter is missing.");
            return;
        }

        if (!_adapter.TryResolveStep(step, out OnboardingResolvedTarget resolvedTarget))
        {
            HideMask();
            _adapter.ClearStepAction();
            Log($"Step '{step.StepId}' could not resolve its runtime target.");
            return;
        }

        _currentResolvedTarget = resolvedTarget;
        ApplyStepPresentation(step, resolvedTarget);
        ApplyStepAction(step, resolvedTarget);
    }

    private void ApplyStepPresentation(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget)
    {
        if (tutorialMask == null)
        {
            Log($"Tutorial mask is missing; step '{step.StepId}' will not render focus.");
            return;
        }

        switch (step.Focus)
        {
            case OnboardingConfigSO.FocusMode.TargetTransform:
                if (!resolvedTarget.HasTransform)
                {
                    HideMask();
                    Log($"Step '{step.StepId}' resolved no transform target.");
                    return;
                }

                tutorialMask.ShowForTarget(resolvedTarget.TargetTransform, refreshNow: true);
                tutorialMask.SetTrackingEnabled(step.TrackTargetEveryFrame, refreshNow: false);
                Log(BuildResolvedTargetLogMessage(step, resolvedTarget, "transform"));
                break;

            case OnboardingConfigSO.FocusMode.WorldArea:
                Vector3 focusCenter = resolvedTarget.WorldPosition + step.FocusWorldOffset;
                Vector3 focusSize = resolvedTarget.FocusWorldSize;
                if (focusSize.sqrMagnitude <= 0.0001f)
                {
                    focusSize = step.WorldFocusSize;
                }

                tutorialMask.ShowForWorldFocus(focusCenter, focusSize, refreshNow: true);
                tutorialMask.SetTrackingEnabled(step.TrackTargetEveryFrame, refreshNow: false);
                Log($"Step '{step.StepId}' focusing world area for target '{resolvedTarget.Id}'.");
                break;

            case OnboardingConfigSO.FocusMode.None:
            default:
                HideMask();
                break;
        }
    }

    private void ApplyStepAction(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget)
    {
        if (_adapter == null)
        {
            return;
        }

        if (_adapter.TryBindStepAction(step, resolvedTarget))
        {
            return;
        }

        _adapter.ClearStepAction();
        Log(
            $"Step '{step.StepId}' uses runtime action but target '{resolvedTarget.Id}' " +
            "does not expose a valid interaction binding.");
    }

    private void HandlePanelActionButtonClicked()
    {
        if (_activeLevel == null || _currentStep == null)
        {
            return;
        }

        if (_currentStep.Advance != OnboardingConfigSO.AdvanceMode.Manual)
        {
            return;
        }

        Log($"Manual next requested for step '{_currentStep.StepId}'.");
        NextStep();
    }

    private void NextStep()
    {
        if (_activeLevel == null)
        {
            return;
        }

        if (_adapter == null || !_adapter.IsReady)
        {
            Log("Cannot advance onboarding step because onboarding adapter is not ready.");
            return;
        }

        int nextIndex = FindNextValidStepIndex(_currentStepIndex);
        if (nextIndex < 0)
        {
            CompleteFlow();
            return;
        }

        ScheduleStep(nextIndex);
    }

    private void CompleteFlow()
    {
        CancelScheduledStep();
        ClearPresentation();
        ResetStepRuntimeState();
        _activeLevel = null;
        Log("Completed onboarding flow for current level.");
    }

    private void ResetStepRuntimeState()
    {
        _adapter?.ClearStepAction();
        _currentResolvedTarget = default;
        _currentStep = null;
        _currentStepIndex = -1;
    }

    private void ResetStepState()
    {
        CancelScheduledStep();
        ResetStepRuntimeState();
    }

    private string BuildResolvedTargetLogMessage(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget, string focusType)
    {
        if (resolvedTarget.HasGridPosition)
        {
            return
                $"Step '{step.StepId}' focusing {focusType} target '{resolvedTarget.Id}' " +
                $"at grid ({resolvedTarget.GridPosition.x}, {resolvedTarget.GridPosition.y}).";
        }

        return
            $"Step '{step.StepId}' focusing {focusType} target '{resolvedTarget.Id}' " +
            $"at world {resolvedTarget.WorldPosition}.";
    }

    private void ResolveTutorialMaskIfNeeded()
    {
        LogIfMissingReference(tutorialMask, nameof(tutorialMask));
    }

    private void LogIfMissingReference(UnityEngine.Object reference, string referenceName)
    {
        if (reference != null)
        {
            return;
        }

        Log($"Missing reference: {referenceName}.");
    }

    private void HideMask()
    {
        if (tutorialMask != null)
        {
            tutorialMask.HideMask(clearTarget: true);
        }
    }

    private void ShowPanel(OnboardingConfigSO.StepData step)
    {
        if (panelTap == null || step == null)
        {
            return;
        }

        string title = step.Title?.Trim() ?? string.Empty;
        string description = step.Description?.Trim() ?? string.Empty;
        string message;

        if (string.IsNullOrWhiteSpace(title))
        {
            message = description;
        }
        else if (string.IsNullOrWhiteSpace(description))
        {
            message = title;
        }
        else
        {
            message = $"{title}\n{description}";
        }

        bool isManual = step.Advance == OnboardingConfigSO.AdvanceMode.Manual;
        panelTap.Render(new PanelTap.ViewData
        {
            Message = message,
            ShowMessage = !string.IsNullOrWhiteSpace(message),
            ShowActionButton = isManual,
            ActionButtonText = step.ActionButtonText,
            ActionButtonInteractable = isManual,
        });
        panelTap.SetVisible(true);
    }

    private void HidePanel()
    {
        if (panelTap != null)
        {
            panelTap.SetVisible(false);
        }
    }

    private void ClearPresentation()
    {
        HideMask();
        HidePanel();
    }

    private void ClearActiveState()
    {
        ResetStepState();
        _activeLevel = null;
    }

    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[{nameof(OnboardingService)}] {message}", this);
        }
    }
}

public static class OnboardingRuntimeSettings
{
    private const string EnabledPlayerPrefsKey = "cheat.onboarding.enabled";

    public static event Action<bool> EnabledChanged;

    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(EnabledPlayerPrefsKey, 1) != 0;
        set
        {
            bool normalizedValue = value;
            if (normalizedValue == IsEnabled)
            {
                return;
            }

            PlayerPrefs.SetInt(EnabledPlayerPrefsKey, normalizedValue ? 1 : 0);
            PlayerPrefs.Save();
            EnabledChanged?.Invoke(normalizedValue);
        }
    }
}
