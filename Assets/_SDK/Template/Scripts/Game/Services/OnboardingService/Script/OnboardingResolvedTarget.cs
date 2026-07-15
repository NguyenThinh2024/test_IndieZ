using UnityEngine;

public readonly struct OnboardingResolvedTarget
{
    public string Id { get; }
    public string DisplayName { get; }
    public string ResolvedKind { get; }
    public Object InteractionTarget { get; }
    public Transform TargetTransform { get; }
    public Vector3 WorldPosition { get; }
    public Vector3 FocusWorldSize { get; }
    public Vector2Int GridPosition { get; }
    public bool HasGridPosition { get; }
    public bool HasTransform => TargetTransform != null;
    public bool HasInteractionTarget => InteractionTarget != null;

    public OnboardingResolvedTarget(
        string id,
        string displayName,
        string resolvedKind,
        Object interactionTarget,
        Transform targetTransform,
        Vector3 worldPosition,
        Vector3 focusWorldSize,
        Vector2Int gridPosition,
        bool hasGridPosition)
    {
        Id = id;
        DisplayName = displayName;
        ResolvedKind = resolvedKind;
        InteractionTarget = interactionTarget;
        TargetTransform = targetTransform;
        WorldPosition = worldPosition;
        FocusWorldSize = focusWorldSize;
        GridPosition = gridPosition;
        HasGridPosition = hasGridPosition;
    }
}
