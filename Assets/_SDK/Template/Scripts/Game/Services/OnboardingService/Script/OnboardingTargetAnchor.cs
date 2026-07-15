using UnityEngine;

[DisallowMultipleComponent]
public class OnboardingTargetAnchor : MonoBehaviour
{
    [SerializeField] private string anchorId = string.Empty;
    [SerializeField] private string displayName = string.Empty;
    [Tooltip("Scene object that should be highlighted for this anchor. Leave empty to use the same GameObject.")]
    [SerializeField] private Transform targetTransform;
    [Tooltip("Optional interaction object used by the resolver. Can be a Component or GameObject from another target.")]
    [SerializeField] private UnityEngine.Object interactionTargetOverride;

    public string AnchorId => anchorId;
    public string DisplayName => displayName;
    public Transform TargetTransform => targetTransform != null ? targetTransform : transform;
    public UnityEngine.Object InteractionTarget => interactionTargetOverride != null ? interactionTargetOverride : null;
    public string ResolvedDisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            Transform target = TargetTransform;
            if (target != null)
            {
                return target.gameObject.name;
            }

            return gameObject.name;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(anchorId))
        {
            anchorId = gameObject.name;
        }
    }
#endif
}
