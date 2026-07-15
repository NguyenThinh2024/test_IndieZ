using UnityEngine;
using Sirenix.OdinInspector;

public class TutorialStateTargetMask : MonoBehaviour
{
    public Transform Target => target;
    public bool HasManualWorldFocus => useManualWorldFocus;

    [FoldoutGroup("Target"), SerializeField] private Transform target = null;
    [FoldoutGroup("Target"), SerializeField] private Camera worldCamera = null;
    [FoldoutGroup("Target"), SerializeField] private RectTransform canvasRoot = null;
    [FoldoutGroup("Target"), SerializeField] private bool trackEveryFrame = true;
    [FoldoutGroup("Target"), SerializeField] private bool useManualWorldFocus = false;
    [FoldoutGroup("Target"), SerializeField] private Vector3 manualWorldFocusCenter = Vector3.zero;
    [FoldoutGroup("Target"), SerializeField] private Vector3 manualWorldFocusSize = Vector3.one;

    [FoldoutGroup("Focus Area"), SerializeField, Min(0f)] private float padding = 24f;
    [FoldoutGroup("Focus Area"), SerializeField, Min(1f)] private float minimumSize = 48f;
    [FoldoutGroup("Focus Area"), SerializeField] private Vector2 focusOffset = Vector2.zero;
    [FoldoutGroup("Focus Area"), SerializeField] private Vector2 focusSizeMultiplier = new Vector2(0.01f, 0.49f);
    [FoldoutGroup("Focus Area"), SerializeField] private Vector2 focusExtraSize = Vector2.zero;
    [FoldoutGroup("Focus Area"), SerializeField] private bool useManualRectSize = false;
    [FoldoutGroup("Focus Area"), ShowIf(nameof(useManualRectSize)), SerializeField] private Vector2 manualRectSize = new Vector2(128f, 128f);
    [FoldoutGroup("Focus Area"), SerializeField] private bool useRendererBounds = true;
    [FoldoutGroup("Focus Area"), SerializeField] private bool useColliderBounds = true;

    [FoldoutGroup("Overlay"), SerializeField] private TutorialMaskVisual maskVisual = null;

    private readonly Vector3[] boundsCorners = new Vector3[8];

    private void Awake()
    {
        EnsureVisuals();
        RefreshMask();
    }

    private void OnEnable()
    {
        EnsureVisuals();
        SetVisualsActive(true);
        RefreshMask();
    }

    private void LateUpdate()
    {
        if (trackEveryFrame)
        {
            RefreshMask();
        }
    }

    private void OnDisable()
    {
        SetVisualsActive(false);
    }

    private void OnValidate()
    {
        SanitizeFocusAreaSettings();
        EnsureVisuals();
        RefreshMask();
    }

    [Button("Refresh Mask Now"), FoldoutGroup("Overlay")]
    [ContextMenu("Refresh Mask")]
    public void RefreshMask()
    {
        SanitizeFocusAreaSettings();
        EnsureVisuals();
        ResolveReferencesIfNeeded();

        if (!TryGetTargetCanvasRect(out Rect focusRect))
        {
            SetVisualsActive(false);
            return;
        }

        SetVisualsActive(true);
        ApplyVisualSettings();
        ApplyMaskLayout(focusRect);
    }

    public void SetTarget(Transform newTarget, bool refreshNow = true)
    {
        useManualWorldFocus = false;
        target = newTarget;

        if (refreshNow)
        {
            RefreshMask();
        }
    }

    public void ClearTarget()
    {
        useManualWorldFocus = false;
        target = null;
        SetVisualsActive(false);
    }

    public void ShowForTarget(Transform newTarget, bool refreshNow = true)
    {
        gameObject.SetActive(true);
        SetTarget(newTarget, refreshNow);
    }

    public void HideMask(bool clearTarget = true)
    {
        if (clearTarget)
        {
            useManualWorldFocus = false;
            target = null;
        }

        SetVisualsActive(false);
        gameObject.SetActive(false);
    }

    public void SetTrackingEnabled(bool enabled, bool refreshNow = true)
    {
        trackEveryFrame = enabled;

        if (refreshNow)
        {
            RefreshMask();
        }
    }

    public void SetManualWorldFocus(Vector3 worldCenter, Vector3 worldSize, bool refreshNow = true)
    {
        useManualWorldFocus = true;
        target = null;
        manualWorldFocusCenter = worldCenter;
        manualWorldFocusSize = new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(worldSize.x)),
            Mathf.Max(0.01f, Mathf.Abs(worldSize.y)),
            Mathf.Max(0.01f, Mathf.Abs(worldSize.z)));

        if (refreshNow)
        {
            RefreshMask();
        }
    }

    public void ShowForWorldFocus(Vector3 worldCenter, Vector3 worldSize, bool refreshNow = true)
    {
        gameObject.SetActive(true);
        SetManualWorldFocus(worldCenter, worldSize, refreshNow);
    }

    public bool TryGetFocusCanvasCenter(out Vector2 canvasCenter)
    {
        canvasCenter = Vector2.zero;

        SanitizeFocusAreaSettings();
        EnsureVisuals();
        ResolveReferencesIfNeeded();

        if (!TryGetTargetCanvasRect(out Rect focusRect))
        {
            return false;
        }

        canvasCenter = focusRect.center;
        return true;
    }

    private void ResolveReferencesIfNeeded()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (canvasRoot == null)
        {
            canvasRoot = transform as RectTransform;
        }
    }

    private bool TryGetTargetCanvasRect(out Rect focusRect)
    {
        focusRect = default;
        if (canvasRoot == null || worldCamera == null)
        {
            return false;
        }

        if (!TryGetFocusBounds(out Bounds bounds))
        {
            return false;
        }

        FillBoundsCorners(bounds, boundsCorners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        bool hasVisiblePoint = false;

        Camera eventCamera = ResolveCanvasEventCamera();
        for (int i = 0; i < boundsCorners.Length; i++)
        {
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(boundsCorners[i]);
            if (screenPoint.z <= 0f)
            {
                continue;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPoint, eventCamera, out Vector2 localPoint))
            {
                continue;
            }

            hasVisiblePoint = true;
            minX = Mathf.Min(minX, localPoint.x);
            minY = Mathf.Min(minY, localPoint.y);
            maxX = Mathf.Max(maxX, localPoint.x);
            maxY = Mathf.Max(maxY, localPoint.y);
        }

        if (!hasVisiblePoint)
        {
            return false;
        }

        float width = Mathf.Max(minimumSize, (maxX - minX) + (padding * 2f));
        float height = Mathf.Max(minimumSize, (maxY - minY) + (padding * 2f));
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

        width = useManualRectSize
            ? Mathf.Max(minimumSize, manualRectSize.x)
            : Mathf.Max(minimumSize, (width * focusSizeMultiplier.x) + focusExtraSize.x);

        height = useManualRectSize
            ? Mathf.Max(minimumSize, manualRectSize.y)
            : Mathf.Max(minimumSize, (height * focusSizeMultiplier.y) + focusExtraSize.y);

        center += focusOffset;

        focusRect = new Rect(
            center.x - (width * 0.5f),
            center.y - (height * 0.5f),
            width,
            height);

        Rect canvasRect = canvasRoot.rect;
        focusRect.xMin = Mathf.Clamp(focusRect.xMin, canvasRect.xMin, canvasRect.xMax);
        focusRect.xMax = Mathf.Clamp(focusRect.xMax, canvasRect.xMin, canvasRect.xMax);
        focusRect.yMin = Mathf.Clamp(focusRect.yMin, canvasRect.yMin, canvasRect.yMax);
        focusRect.yMax = Mathf.Clamp(focusRect.yMax, canvasRect.yMin, canvasRect.yMax);

        return focusRect.width > 0.01f && focusRect.height > 0.01f;
    }

    private void SanitizeFocusAreaSettings()
    {
        focusSizeMultiplier = new Vector2(
            Mathf.Max(0.01f, focusSizeMultiplier.x),
            Mathf.Max(0.01f, focusSizeMultiplier.y));

        manualRectSize = new Vector2(
            Mathf.Max(minimumSize, Mathf.Abs(manualRectSize.x)),
            Mathf.Max(minimumSize, Mathf.Abs(manualRectSize.y)));
    }

    private bool TryGetFocusBounds(out Bounds bounds)
    {
        bounds = default;

        if (useManualWorldFocus)
        {
            bounds = new Bounds(manualWorldFocusCenter, manualWorldFocusSize);
            return true;
        }

        if (target == null)
        {
            return false;
        }

        bool hasBounds = false;

        if (useRendererBounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (useColliderBounds)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(target.position, Vector3.one);
            hasBounds = true;
        }

        return hasBounds;
    }

    private void FillBoundsCorners(Bounds bounds, Vector3[] corners)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(max.x, max.y, min.z);
        corners[4] = new Vector3(min.x, min.y, max.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(min.x, max.y, max.z);
        corners[7] = new Vector3(max.x, max.y, max.z);
    }

    private void ApplyMaskLayout(Rect focusRect)
    {
        if (maskVisual != null)
        {
            maskVisual.ApplyFocusRect(canvasRoot, focusRect);
        }
    }

    private void EnsureVisuals()
    {
        ResolveReferencesIfNeeded();
    }

    private void ApplyVisualSettings()
    {
        if (maskVisual != null)
        {
            maskVisual.ApplyStyle();
        }
    }

    private void SetVisualsActive(bool visible)
    {
        if (maskVisual != null)
        {
            maskVisual.SetVisualsActive(visible);
        }
    }

    private Camera ResolveCanvasEventCamera()
    {
        Canvas canvas = canvasRoot != null ? canvasRoot.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return canvas.worldCamera;
        }

        return null;
    }

    private string GetCanvasRootWarning()
    {
        if (canvasRoot == null)
        {
            return "canvasRoot is not assigned. It should point to a full-screen RectTransform inside the Canvas.";
        }

        Rect rect = canvasRoot.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return "canvasRoot has an invalid RectTransform size. Use a full-screen panel/canvas root.";
        }

        Canvas canvas = canvasRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return "canvasRoot is not under a Canvas. Tutorial mask visuals need a full-screen Canvas hierarchy.";
        }

        RectTransform canvasTransform = canvas.transform as RectTransform;
        if (canvasTransform != null)
        {
            Rect canvasRect = canvasTransform.rect;
            bool looksMuchSmaller =
                rect.width < canvasRect.width * 0.5f ||
                rect.height < canvasRect.height * 0.5f;

            if (looksMuchSmaller)
            {
                return "canvasRoot looks smaller than the parent Canvas. Point it to a full-screen panel instead of a small child RectTransform.";
            }
        }

        return string.Empty;
    }

    private string GetWorldCameraWarning()
    {
        Canvas canvas = canvasRoot != null ? canvasRoot.GetComponentInParent<Canvas>() : null;

        if (worldCamera == null)
        {
            return "worldCamera is not assigned. The target bounds cannot be projected to the mask correctly.";
        }

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
            canvas.worldCamera == null)
        {
            return "This Canvas is not Screen Space Overlay but Canvas.worldCamera is missing. Screen-to-canvas conversion may be wrong.";
        }

        return string.Empty;
    }

    private string GetOverlayImageWarning() => string.Empty;
}
