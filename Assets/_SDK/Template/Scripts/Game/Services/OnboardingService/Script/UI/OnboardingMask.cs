using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnboardingMask : MonoBehaviour, IPointerClickHandler
{
    public event System.Action HoleTapped;

    private const string CutoutShaderName = "UI/Circle Cutout Overlay";
    private static readonly int OverlayColorId = Shader.PropertyToID("_OverlayColor");
    private static readonly int HoleCenterId = Shader.PropertyToID("_HoleCenter");
    private static readonly int HoleRadiusId = Shader.PropertyToID("_HoleRadius");
    private static readonly int HoleAspectId = Shader.PropertyToID("_HoleAspect");
    private static readonly int CanvasSizeId = Shader.PropertyToID("_CanvasSize");
    private static readonly int HoleRadiusPxId = Shader.PropertyToID("_HoleRadiusPx");
    private static readonly int HoleSizePxId = Shader.PropertyToID("_HoleSizePx");
    private static readonly int ShapeTypeId = Shader.PropertyToID("_ShapeType");
    private static readonly int MaskScaleXYId = Shader.PropertyToID("_MaskScaleXY");
    private static readonly int MaskScaleXId = Shader.PropertyToID("_MaskScaleX");
    private static readonly int MaskScaleYId = Shader.PropertyToID("_MaskScaleY");
    private static readonly int HoleBlurId = Shader.PropertyToID("_HoleBlur");
    private static readonly int BorderWidthId = Shader.PropertyToID("_BorderWidth");
    private static readonly int BorderBlurId = Shader.PropertyToID("_BorderBlur");
    private static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");

    [FoldoutGroup("Refs"), SerializeField] private Image overlayImage;
    [FoldoutGroup("Refs"), SerializeField] private Material materialTemplate;

    [FoldoutGroup("Style"), SerializeField] private bool blockRaycasts = true;
    [FoldoutGroup("Style"), SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.72f);
    [FoldoutGroup("Style"), SerializeField] private Color borderColor = Color.white;
    [FoldoutGroup("Style"), SerializeField, Min(0f)] private float borderWidthUv = 0.015f;
    [FoldoutGroup("Style"), SerializeField, Min(0.0001f)] private float borderBlur = 0.01f;
    [FoldoutGroup("Style"), SerializeField, Min(0.0001f)] private float holeBlur = 0.02f;
    [FoldoutGroup("Style"), SerializeField] private bool autoFitMaskToScreen = true;
    [FoldoutGroup("Style"), SerializeField] private Vector2 manualMaskScale = Vector2.one;
    [FoldoutGroup("Animation"), SerializeField] private bool animateCircle = true;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float circleStepStartDuration = 0.4f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float circleStepEndDuration = 0.28f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float circleClickDuration = 0.2f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float circleFinalExpandDuration = 0.55f;
    [FoldoutGroup("Animation"), SerializeField, Min(1f)] private float circleFinalExpandScreenMultiplier = 1.35f;

    private Material _runtimeMaterial;
    private Material _originalMaterial;
    private readonly Vector3[] _corners = new Vector3[8];
    private RectTransform _canvasRoot;
    private Transform _target;
    private bool _useManualWorldFocus;
    private Vector3 _manualWorldFocusCenter;
    private Vector3 _manualWorldFocusSize = Vector3.one;
    private bool _overlayOnlyMode;
    private Rect _lastFocusRect;
    private bool _hasLastFocusRect;
    private float _currentFocusRadiusPx;
    private Vector2 _currentFocusHalfSizePx;
    private OnboardingConfigSO.FocusShape _currentShape = OnboardingConfigSO.FocusShape.Circle;
    private Tween _circleRadiusTween;
    private Tween _circleBorderTween;
    private Tween _hideMaskTween;
    private bool _isFinalStep;
    private Coroutine _pendingHoleTapRoutine;

    private const float FocusPadding = 24f;
    private const float FocusMinimumSize = 48f;
    private const bool UseRendererBounds = true;
    private const bool UseColliderBounds = true;

    private void Awake()
    {
        EnsureMaterial();
        ApplyStyle();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureMaterial();
        ApplyStyle();
        RefreshMask();
    }

    private void OnDisable()
    {
        KillCircleTweens();
        RestoreMaterial();
    }

    private void OnDestroy()
    {
        KillCircleTweens();
        RestoreMaterial();
    }

    private void OnValidate()
    {
        _manualWorldFocusSize = new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(_manualWorldFocusSize.x)),
            Mathf.Max(0.01f, Mathf.Abs(_manualWorldFocusSize.y)),
            Mathf.Max(0.01f, Mathf.Abs(_manualWorldFocusSize.z)));
        ResolveReferences();
        EnsureMaterial();
        ApplyStyle();
        RefreshMask();
    }

    public void SetVisible(bool visible)
    {
        if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(visible);
        }
    }

    public void ApplyStyle()
    {
        if (overlayImage == null || _runtimeMaterial == null)
        {
            return;
        }

        overlayImage.color = Color.white;
        overlayImage.raycastTarget = blockRaycasts;
        _runtimeMaterial.SetColor(OverlayColorId, overlayColor);
        _runtimeMaterial.SetColor(BorderColorId, borderColor);
        _runtimeMaterial.SetFloat(BorderWidthId, Mathf.Max(0f, borderWidthUv));
        _runtimeMaterial.SetFloat(BorderBlurId, Mathf.Max(0.0001f, borderBlur));
        _runtimeMaterial.SetFloat(HoleBlurId, Mathf.Max(0.0001f, holeBlur));
        _runtimeMaterial.SetFloat(ShapeTypeId, _currentShape == OnboardingConfigSO.FocusShape.Rectangle ? 1f : 0f);
    }

    public void ApplyFocusRect(RectTransform canvasRoot, Rect focusRect)
    {
        if (_runtimeMaterial == null || canvasRoot == null)
        {
            return;
        }

        Rect canvasRect = canvasRoot.rect;
        if (canvasRect.width <= Mathf.Epsilon || canvasRect.height <= Mathf.Epsilon)
        {
            return;
        }

        float centerU = Mathf.InverseLerp(canvasRect.xMin, canvasRect.xMax, focusRect.center.x);
        float centerV = Mathf.InverseLerp(canvasRect.yMin, canvasRect.yMax, focusRect.center.y);
        float radiusPx = Mathf.Max(focusRect.width, focusRect.height) * 0.5f;
        Vector2 halfSizePx = new Vector2(
            Mathf.Max(0.0001f, focusRect.width * 0.5f),
            Mathf.Max(0.0001f, focusRect.height * 0.5f));
        float aspect = canvasRect.width / Mathf.Max(canvasRect.height, Mathf.Epsilon);
        float radiusUvLegacy = radiusPx / Mathf.Max(canvasRect.height, Mathf.Epsilon);

        _runtimeMaterial.SetVector(HoleCenterId, new Vector4(centerU, centerV, 0f, 0f));
        _runtimeMaterial.SetVector(CanvasSizeId, new Vector4(canvasRect.width, canvasRect.height, 0f, 0f));
        _runtimeMaterial.SetFloat(HoleRadiusPxId, Mathf.Max(0.0001f, radiusPx));
        _runtimeMaterial.SetVector(HoleSizePxId, new Vector4(halfSizePx.x, halfSizePx.y, 0f, 0f));
        _runtimeMaterial.SetFloat(HoleAspectId, aspect);
        _runtimeMaterial.SetFloat(HoleRadiusId, Mathf.Max(0.0001f, radiusUvLegacy));
        Vector2 resolvedMaskScale = ResolveMaskScale(canvasRect);
        _runtimeMaterial.SetVector(MaskScaleXYId, new Vector4(
            Mathf.Max(0.0001f, resolvedMaskScale.x),
            Mathf.Max(0.0001f, resolvedMaskScale.y),
            0f,
            0f));
        _runtimeMaterial.SetFloat(MaskScaleXId, Mathf.Max(0.0001f, resolvedMaskScale.x));
        _runtimeMaterial.SetFloat(MaskScaleYId, Mathf.Max(0.0001f, resolvedMaskScale.y));
    }

    private Vector2 ResolveMaskScale(Rect canvasRect)
    {
        Vector2 baseScale = new Vector2(
            Mathf.Max(0.0001f, manualMaskScale.x),
            Mathf.Max(0.0001f, manualMaskScale.y));

        if (!autoFitMaskToScreen)
        {
            return baseScale;
        }

        // Circle radius is already evaluated in pixel-space in shader,
        // so additional aspect compensation here would distort the hole.
        return baseScale;
    }

    public void ShowForTarget(Transform newTarget, bool refreshNow = true)
    {
        _hideMaskTween?.Kill();
        gameObject.SetActive(true);
        _overlayOnlyMode = false;
        _hasLastFocusRect = false;
        _useManualWorldFocus = false;
        _target = newTarget;
        if (refreshNow)
        {
            RefreshMask();
        }
    }

    public void ShowForWorldFocus(Vector3 worldCenter, Vector3 worldSize, bool refreshNow = true)
    {
        _hideMaskTween?.Kill();
        gameObject.SetActive(true);
        _overlayOnlyMode = false;
        _hasLastFocusRect = false;
        _useManualWorldFocus = true;
        _target = null;
        _manualWorldFocusCenter = worldCenter;
        _manualWorldFocusSize = new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(worldSize.x)),
            Mathf.Max(0.01f, Mathf.Abs(worldSize.y)),
            Mathf.Max(0.01f, Mathf.Abs(worldSize.z)));
        if (refreshNow)
        {
            RefreshMask();
        }
    }

    public void ShowOverlayOnly()
    {
        _hideMaskTween?.Kill();
        ResolveReferences();
        EnsureMaterial();
        ApplyStyle();
        _overlayOnlyMode = true;
        _hasLastFocusRect = false;
        _target = null;
        _useManualWorldFocus = false;
        SetVisible(true);
        gameObject.SetActive(true);
        ApplyOverlayOnlyMaterial();
    }

    public void SetTrackingEnabled(bool enabled, bool refreshNow = true)
    {
        // Kept for compatibility with existing callers.
        if (refreshNow)
        {
            RefreshMask();
        }
    }

    public void HideMask(bool clearTarget = true)
    {
        _hideMaskTween?.Kill();
        if (_pendingHoleTapRoutine != null)
        {
            StopCoroutine(_pendingHoleTapRoutine);
            _pendingHoleTapRoutine = null;
        }
        if (clearTarget)
        {
            _target = null;
            _useManualWorldFocus = false;
        }

        if (animateCircle && _runtimeMaterial != null && _hasLastFocusRect && !_overlayOnlyMode && gameObject.activeInHierarchy)
        {
            float shapeScale = 1f;
            float duration = Mathf.Max(0.05f, circleStepEndDuration);
            _hideMaskTween = DOTween.To(() => shapeScale, value =>
                {
                    shapeScale = value;
                    ApplyShapeScale(shapeScale);
                }, 0.0001f, duration)
                .SetEase(Ease.InSine)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _overlayOnlyMode = false;
                    _hasLastFocusRect = false;
                    SetVisible(false);
                    gameObject.SetActive(false);
                });
            return;
        }

        _overlayOnlyMode = false;
        _hasLastFocusRect = false;
        SetVisible(false);
        gameObject.SetActive(false);
    }

    public void RefreshMask()
    {
        ResolveReferences();
        if (_overlayOnlyMode)
        {
            ApplyStyle();
            SetVisible(true);
            ApplyOverlayOnlyMaterial();
            return;
        }

        if (!TryBuildFocusRect(out Rect focusRect))
        {
            SetVisible(false);
            return;
        }

        ApplyStyle();
        SetVisible(true);
        ApplyFocusRect(_canvasRoot, focusRect);
        _lastFocusRect = focusRect;
        _hasLastFocusRect = true;
        _currentFocusRadiusPx = Mathf.Max(focusRect.width, focusRect.height) * 0.5f;
        _currentFocusHalfSizePx = new Vector2(
            Mathf.Max(0.0001f, focusRect.width * 0.5f),
            Mathf.Max(0.0001f, focusRect.height * 0.5f));
        PlayCircleStepStartAnimation();
    }

    public bool TryGetFocusCenterLocalPoint(out Vector2 centerLocalPoint)
    {
        centerLocalPoint = Vector2.zero;
        if (_overlayOnlyMode || !_hasLastFocusRect)
        {
            return false;
        }

        centerLocalPoint = _lastFocusRect.center;
        return true;
    }

    public bool TryGetFocusCenterScreenPoint(out Vector2 screenPoint, out Camera eventCamera)
    {
        screenPoint = Vector2.zero;
        eventCamera = null;
        if (_overlayOnlyMode || !_hasLastFocusRect || _canvasRoot == null)
        {
            return false;
        }

        eventCamera = ResolveCanvasEventCamera();
        Vector3 worldPoint = _canvasRoot.TransformPoint(_lastFocusRect.center);
        screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);
        return true;
    }

    public void SetIsFinalStep(bool isFinalStep)
    {
        _isFinalStep = isFinalStep;
    }

    public void SetShape(OnboardingConfigSO.FocusShape shape)
    {
        _currentShape = shape;
        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.SetFloat(ShapeTypeId, _currentShape == OnboardingConfigSO.FocusShape.Rectangle ? 1f : 0f);
        }
    }

    private void ResolveReferences()
    {
        if (_canvasRoot == null)
        {
            _canvasRoot = transform as RectTransform;
        }
    }

    private void EnsureMaterial()
    {
        if (overlayImage == null || _runtimeMaterial != null)
        {
            return;
        }

        Material source = materialTemplate;
        if (source == null)
        {
            Shader shader = Shader.Find(CutoutShaderName);
            if (shader == null)
            {
                return;
            }

            source = new Material(shader);
        }

        _originalMaterial = overlayImage.material;
        _runtimeMaterial = new Material(source)
        {
            name = source.name + " (Runtime)"
        };
        overlayImage.material = _runtimeMaterial;
    }

    private bool TryBuildFocusRect(out Rect focusRect)
    {
        focusRect = default;
        Camera worldCamera = Camera.main;
        if (_canvasRoot == null || worldCamera == null || !TryGetFocusBounds(out Bounds bounds))
        {
            return false;
        }

        FillBoundsCorners(bounds, _corners);
        Camera eventCamera = ResolveCanvasEventCamera();

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        bool hasPoint = false;

        for (int i = 0; i < _corners.Length; i++)
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(_corners[i]);
            if (screen.z <= 0f)
            {
                continue;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRoot, screen, eventCamera, out Vector2 local))
            {
                continue;
            }

            hasPoint = true;
            minX = Mathf.Min(minX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxX = Mathf.Max(maxX, local.x);
            maxY = Mathf.Max(maxY, local.y);
        }

        if (!hasPoint)
        {
            return false;
        }

        float width = Mathf.Max(FocusMinimumSize, (maxX - minX) + (FocusPadding * 2f));
        float height = Mathf.Max(FocusMinimumSize, (maxY - minY) + (FocusPadding * 2f));
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

        focusRect = new Rect(center.x - (width * 0.5f), center.y - (height * 0.5f), width, height);
        return focusRect.width > 0.01f && focusRect.height > 0.01f;
    }

    private bool TryGetFocusBounds(out Bounds bounds)
    {
        bounds = default;
        if (_useManualWorldFocus)
        {
            bounds = new Bounds(_manualWorldFocusCenter, _manualWorldFocusSize);
            return true;
        }

        if (_target == null)
        {
            return false;
        }

        bool hasBounds = false;
        if (UseRendererBounds)
        {
            Renderer[] renderers = _target.GetComponentsInChildren<Renderer>(true);
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

        if (UseColliderBounds)
        {
            Collider[] colliders = _target.GetComponentsInChildren<Collider>(true);
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
            bounds = new Bounds(_target.position, Vector3.one);
        }

        return true;
    }

    private Camera ResolveCanvasEventCamera()
    {
        Canvas canvas = _canvasRoot != null ? _canvasRoot.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return canvas.worldCamera;
        }

        return null;
    }

    private void ApplyOverlayOnlyMaterial()
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        _runtimeMaterial.SetVector(HoleCenterId, new Vector4(0.5f, 0.5f, 0f, 0f));
        _runtimeMaterial.SetFloat(HoleRadiusPxId, 0.0001f);
        _runtimeMaterial.SetFloat(HoleRadiusId, 0.0001f);
        _runtimeMaterial.SetFloat(BorderWidthId, 0f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_overlayOnlyMode || !_hasLastFocusRect || _canvasRoot == null || eventData == null)
        {
            return;
        }

        Camera eventCamera = ResolveCanvasEventCamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRoot, eventData.position, eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 center = _lastFocusRect.center;
        Vector2 delta = localPoint - center;
        if (_currentShape == OnboardingConfigSO.FocusShape.Rectangle)
        {
            if (Mathf.Abs(delta.x) > _currentFocusHalfSizePx.x || Mathf.Abs(delta.y) > _currentFocusHalfSizePx.y)
            {
                return;
            }
        }
        else
        {
            float radius = Mathf.Max(_lastFocusRect.width, _lastFocusRect.height) * 0.5f;
            if (delta.sqrMagnitude > radius * radius)
            {
                return;
            }
        }

        float clickAnimDuration = PlayCircleClickAnimation();
        if (_pendingHoleTapRoutine != null)
        {
            StopCoroutine(_pendingHoleTapRoutine);
        }

        _pendingHoleTapRoutine = StartCoroutine(InvokeHoleTappedAfterDelay(clickAnimDuration));
    }

    private void PlayCircleStepStartAnimation()
    {
        if (!animateCircle || _runtimeMaterial == null)
        {
            return;
        }

        float shapeScale = 0.0001f;
        ApplyShapeScale(shapeScale);
        _circleRadiusTween?.Kill();
        _circleRadiusTween = DOTween.To(() => shapeScale, value =>
            {
                shapeScale = value;
                ApplyShapeScale(shapeScale);
            }, 1f, Mathf.Max(0.05f, circleStepStartDuration))
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private float PlayCircleClickAnimation()
    {
        if (!animateCircle || _runtimeMaterial == null)
        {
            return 0f;
        }

        float shapeScale = 1f;
        float clickDuration = Mathf.Max(0.03f, _isFinalStep ? circleFinalExpandDuration : circleClickDuration);
        _circleRadiusTween?.Kill();

        if (_isFinalStep)
        {
            float targetScale = ResolveScreenCoverScale() * Mathf.Max(1f, circleFinalExpandScreenMultiplier);
            _circleRadiusTween = DOTween.To(() => shapeScale, value =>
            {
                shapeScale = value;
                ApplyShapeScale(shapeScale);
            }, targetScale, clickDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
            return clickDuration;
        }

        _circleRadiusTween = DOTween.To(() => shapeScale, value =>
            {
                shapeScale = value;
                ApplyShapeScale(shapeScale);
            }, 0.0001f, clickDuration)
            .SetEase(Ease.InSine)
            .SetUpdate(true);
        return clickDuration;
    }

    private float ResolveScreenCoverScale()
    {
        Rect canvasRect = _canvasRoot != null ? _canvasRoot.rect : new Rect(0f, 0f, 1080f, 1920f);
        float width = Mathf.Max(1f, canvasRect.width);
        float height = Mathf.Max(1f, canvasRect.height);
        float diagonal = Mathf.Sqrt((width * width) + (height * height));
        float baseSize = _currentShape == OnboardingConfigSO.FocusShape.Rectangle
            ? Mathf.Max(_currentFocusHalfSizePx.x, _currentFocusHalfSizePx.y)
            : Mathf.Max(0.0001f, _currentFocusRadiusPx);
        return diagonal / Mathf.Max(baseSize, 0.0001f);
    }

    private void ApplyShapeScale(float scale)
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        float safeScale = Mathf.Max(0.0001f, scale);
        if (_currentShape == OnboardingConfigSO.FocusShape.Rectangle)
        {
            float halfX = Mathf.Max(0.0001f, _currentFocusHalfSizePx.x * safeScale);
            float halfY = Mathf.Max(0.0001f, _currentFocusHalfSizePx.y * safeScale);
            _runtimeMaterial.SetVector(HoleSizePxId, new Vector4(halfX, halfY, 0f, 0f));
            _runtimeMaterial.SetFloat(HoleRadiusPxId, Mathf.Max(halfX, halfY));
            _runtimeMaterial.SetFloat(HoleRadiusId, Mathf.Max(halfX, halfY) / Mathf.Max(_canvasRoot != null ? _canvasRoot.rect.height : 1f, 1f));
        }
        else
        {
            float radius = Mathf.Max(0.0001f, _currentFocusRadiusPx * safeScale);
            _runtimeMaterial.SetFloat(HoleRadiusPxId, radius);
            _runtimeMaterial.SetVector(HoleSizePxId, new Vector4(radius, radius, 0f, 0f));
            _runtimeMaterial.SetFloat(HoleRadiusId, radius / Mathf.Max(_canvasRoot != null ? _canvasRoot.rect.height : 1f, 1f));
        }
    }

    private IEnumerator InvokeHoleTappedAfterDelay(float delaySeconds)
    {
        if (delaySeconds > 0.0001f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

        _pendingHoleTapRoutine = null;
        HoleTapped?.Invoke();
    }

    private void KillCircleTweens()
    {
        _circleRadiusTween?.Kill();
        _circleBorderTween?.Kill();
        _hideMaskTween?.Kill();
        _circleRadiusTween = null;
        _circleBorderTween = null;
        _hideMaskTween = null;

        if (_pendingHoleTapRoutine != null)
        {
            StopCoroutine(_pendingHoleTapRoutine);
            _pendingHoleTapRoutine = null;
        }
    }

    private static void FillBoundsCorners(Bounds bounds, Vector3[] targetCorners)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        targetCorners[0] = new Vector3(min.x, min.y, min.z);
        targetCorners[1] = new Vector3(max.x, min.y, min.z);
        targetCorners[2] = new Vector3(min.x, max.y, min.z);
        targetCorners[3] = new Vector3(max.x, max.y, min.z);
        targetCorners[4] = new Vector3(min.x, min.y, max.z);
        targetCorners[5] = new Vector3(max.x, min.y, max.z);
        targetCorners[6] = new Vector3(min.x, max.y, max.z);
        targetCorners[7] = new Vector3(max.x, max.y, max.z);
    }

    private void RestoreMaterial()
    {
        if (overlayImage != null && _runtimeMaterial != null && overlayImage.material == _runtimeMaterial)
        {
            overlayImage.material = _originalMaterial;
        }

        if (_runtimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(_runtimeMaterial);
        }

        _runtimeMaterial = null;
    }
}
