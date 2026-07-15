using System;
using UnityEngine;

public class OnboardingGameplayAdapter : MonoBehaviour, IOnboardingAdapter
{
    public event Action TapProgressed;

    public bool IsReady => true;

    public bool TryResolveStep(
        OnboardingConfigSO.StepData step,
        out OnboardingResolvedTarget resolvedTarget)
    {
        resolvedTarget = default;
        if (step == null)
        {
            return false;
        }

        switch (step.Target)
        {
            case OnboardingConfigSO.TargetSource.RuntimeTargetId:
                return TryResolveRuntimeTarget(step.RuntimeTargetId, out resolvedTarget);

            case OnboardingConfigSO.TargetSource.WorldPosition:
                resolvedTarget = new OnboardingResolvedTarget(
                    "world_position",
                    "World Position",
                    "WorldPoint",
                    null,
                    null,
                    step.WorldPosition,
                    step.WorldFocusSize,
                    default,
                    hasGridPosition: false);
                return true;

            default:
                return false;
        }
    }

    public bool TryBindStepAction(
        OnboardingConfigSO.StepData step,
        OnboardingResolvedTarget resolvedTarget)
    {
        return step == null || step.Advance != OnboardingConfigSO.AdvanceMode.TapResolvedTarget;
    }

    public void ClearStepAction()
    {
    }

    public void ReportTapProgressed()
    {
        TapProgressed?.Invoke();
    }

    private static bool TryResolveRuntimeTarget(
        string runtimeTargetId,
        out OnboardingResolvedTarget resolvedTarget)
    {
        resolvedTarget = default;
        if (string.IsNullOrWhiteSpace(runtimeTargetId))
        {
            return false;
        }

        OnboardingTargetAnchor anchor = FindTargetAnchor(runtimeTargetId);
        if (anchor == null || anchor.TargetTransform == null)
        {
            return false;
        }

        Transform targetTransform = anchor.TargetTransform;
        UnityEngine.Object interactionTarget = anchor.InteractionTarget;
        if (interactionTarget == null)
        {
            interactionTarget = targetTransform.gameObject;
        }

        resolvedTarget = new OnboardingResolvedTarget(
            runtimeTargetId,
            anchor.ResolvedDisplayName,
            "SceneAnchor",
            interactionTarget,
            targetTransform,
            targetTransform.position,
            Vector3.zero,
            default,
            hasGridPosition: false);
        return true;
    }

    private static OnboardingTargetAnchor FindTargetAnchor(string runtimeTargetId)
    {
        OnboardingTargetAnchor[] anchors = Resources.FindObjectsOfTypeAll<OnboardingTargetAnchor>();
        for (int i = 0; i < anchors.Length; i++)
        {
            OnboardingTargetAnchor anchor = anchors[i];
            if (anchor == null || !anchor.gameObject.scene.IsValid())
            {
                continue;
            }

            if (string.Equals(anchor.AnchorId, runtimeTargetId, StringComparison.Ordinal))
            {
                return anchor;
            }
        }

        return null;
    }
}
