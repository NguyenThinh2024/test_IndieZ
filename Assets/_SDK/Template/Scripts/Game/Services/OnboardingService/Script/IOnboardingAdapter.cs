using System;

public interface IOnboardingAdapter
{
    event Action TapProgressed;

    bool IsReady { get; }

    bool TryResolveStep(OnboardingConfigSO.StepData step, out OnboardingResolvedTarget resolvedTarget);
    bool TryBindStepAction(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget);
    void ClearStepAction();
}
