using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class OnboardingPanelTap : MonoBehaviour
{
    public struct ViewData
    {
        public string Message;
        public bool ShowMessage;
        public bool ShowHandIcon;
    }

    [FoldoutGroup("View"), SerializeField] private CanvasGroup canvasGroup;

    [FoldoutGroup("Text"), SerializeField] private TMP_Text targetText;
    [FoldoutGroup("Text"), SerializeField] private bool autoFindText = true;

    [FoldoutGroup("Icon"), SerializeField] private Transform iconTransform;

    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float showDuration = 0.2f;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float hideDuration = 0.15f;
    [FoldoutGroup("Animation"), SerializeField] private Ease showEase = Ease.OutSine;
    [FoldoutGroup("Animation"), SerializeField] private Ease hideEase = Ease.InSine;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float iconShowDuration = 0.2f;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float iconHideDuration = 0.15f;
    [FoldoutGroup("Animation"), SerializeField] private Ease iconShowEase = Ease.OutBack;
    [FoldoutGroup("Animation"), SerializeField] private Ease iconHideEase = Ease.InBack;
    [FoldoutGroup("Animation"), SerializeField] private float iconHiddenScale = 0.85f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.1f)] private float handTapDownScale = 0.92f;
    [FoldoutGroup("Animation"), SerializeField, Min(1f)] private float handTapUpScale = 1.02f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float handTapDownDuration = 0.14f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float handTapUpDuration = 0.22f;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float handTapLoopInterval = 0.08f;
    [FoldoutGroup("Animation"), SerializeField] private Ease handTapDownEase = Ease.InSine;
    [FoldoutGroup("Animation"), SerializeField] private Ease handTapUpEase = Ease.OutBack;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float handStepStartDuration = 0.16f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float handClickDuration = 0.1f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.05f)] private float handStepCompleteDuration = 0.12f;
    [FoldoutGroup("Animation"), SerializeField, Min(1f)] private float handStepStartScale = 1.08f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.1f)] private float handClickScale = 0.9f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.1f)] private float handStepCompleteScale = 0.8f;
    [FoldoutGroup("Text Animation"), SerializeField, Min(0.05f)] private float textStepStartDuration = 0.18f;
    [FoldoutGroup("Text Animation"), SerializeField, Min(0.05f)] private float textStepEndDuration = 0.12f;
    [FoldoutGroup("Text Animation"), SerializeField, Min(1f)] private float textStepStartOvershoot = 1.06f;
    [FoldoutGroup("Text Animation"), SerializeField, Min(0.7f)] private float textStepEndScale = 0.9f;
    [FoldoutGroup("Text Animation"), SerializeField, Min(0.01f)] private float textPulseAmplitude = 0.04f;
    [FoldoutGroup("Text Animation"), SerializeField, Min(0.2f)] private float textPulseDuration = 0.9f;

    private Vector3 baseIconScale = Vector3.one;
    private Vector3 defaultIconLocalPosition = Vector3.zero;
    private Vector3 baseIconLocalPosition = Vector3.zero;
    private bool isVisible = true;
    private bool isHandIconVisible = true;
    private bool isMessageVisible;
    private Tween visibilityTween;
    private Tween iconFadeTween;
    private Tween iconScaleTween;
    private Sequence handTapLoopSequence;
    private Sequence handOneShotTween;
    private Sequence textStepTween;
    private Tween textPulseTween;
    private Vector3 defaultTextScale = Vector3.one;

    private void Awake()
    {
        ResolveReferences();
        CacheDefaults();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheDefaults();
        ApplyVisibility();
    }

    private void OnDisable()
    {
        visibilityTween?.Kill();
        iconFadeTween?.Kill();
        iconScaleTween?.Kill();
        textStepTween?.Kill();
        handOneShotTween?.Kill();
        StopTextPulse();
        StopHandLoop();
        ResetTextScale();
        ResetIconState();
    }

    private void OnValidate()
    {
        ResolveReferences();
        CacheDefaults();
    }

    private void ResolveText()
    {
        if (targetText == null && autoFindText)
        {
            targetText = GetComponentInChildren<TMP_Text>(true);
        }

        if (targetText != null)
        {
            defaultTextScale = targetText.transform.localScale;
        }
    }

    private void ResolveIconRefs()
    {
        if (iconTransform == null)
        {
            return;
        }
    }

    private void ResolveCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void ResolveReferences()
    {
        ResolveCanvasGroup();
        ResolveText();
        ResolveIconRefs();
    }

    private void CacheDefaults()
    {
        if (iconTransform != null)
        {
            baseIconScale = iconTransform.localScale;
            defaultIconLocalPosition = iconTransform.localPosition;
            baseIconLocalPosition = iconTransform.localPosition;
        }
        
    }

    private void ResetIconState()
    {
        if (iconTransform != null)
        {
            iconTransform.localScale = baseIconScale;
            iconTransform.localPosition = baseIconLocalPosition;
        }
    }

    private void OnDestroy()
    {
        visibilityTween?.Kill();
        iconFadeTween?.Kill();
        iconScaleTween?.Kill();
        textStepTween?.Kill();
        handOneShotTween?.Kill();
        StopTextPulse();
        StopHandLoop();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ApplyVisibility();
    }

    public void Render(ViewData viewData)
    {
        SetMessage(viewData.Message, viewData.ShowMessage);
        SetHandIconVisible(viewData.ShowHandIcon);
    }

    public void SetHandIconVisible(bool visible)
    {
        isHandIconVisible = visible;
        ApplyIconVisibility();
    }

    public void SetMessage(string message, bool visible)
    {
        if (targetText != null)
        {
            targetText.text = message ?? string.Empty;
        }

        if (targetText != null)
        {
            bool shouldShow = visible && !string.IsNullOrWhiteSpace(message);
            isMessageVisible = shouldShow;
            if (shouldShow)
            {
                targetText.gameObject.SetActive(true);
                PlayTextStepStartAnimation();
                StartTextPulse();
            }
            else
            {
                PlayTextStepEndAnimation();
            }
        }
    }

    public void SetHandAnchorLocalPosition(Vector2 localPoint)
    {
        if (iconTransform == null)
        {
            return;
        }

        if (iconTransform is RectTransform iconRect)
        {
            iconRect.anchoredPosition = localPoint;
            baseIconLocalPosition = iconRect.localPosition;
        }
        else
        {
            iconTransform.localPosition = localPoint;
            baseIconLocalPosition = iconTransform.localPosition;
        }
    }

    public void SetHandAnchorScreenPosition(Vector2 screenPoint, Camera eventCamera)
    {
        if (iconTransform == null)
        {
            return;
        }

        RectTransform iconRect = iconTransform as RectTransform;
        RectTransform parentRect = iconRect != null ? iconRect.parent as RectTransform : null;
        if (iconRect == null || parentRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, eventCamera,
                out Vector2 parentLocalPoint))
        {
            return;
        }

        iconRect.anchoredPosition = parentLocalPoint;
        baseIconLocalPosition = iconRect.localPosition;
    }

    public void ResetHandAnchorToDefault()
    {
        if (iconTransform == null)
        {
            return;
        }

        iconTransform.localPosition = defaultIconLocalPosition;
        baseIconLocalPosition = defaultIconLocalPosition;
    }

    public void PlayHandStepStartAnimation()
    {
        if (!CanAnimateHand())
        {
            return;
        }

        handOneShotTween?.Kill();
        StopHandLoop();
        float duration = Mathf.Max(0.05f, handStepStartDuration);
        Vector3 boostedScale = baseIconScale * Mathf.Max(1f, handStepStartScale);
        handOneShotTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(iconTransform.DOScale(boostedScale, duration * 0.5f).SetEase(Ease.OutBack))
            .Append(iconTransform.DOScale(baseIconScale, duration * 0.5f).SetEase(Ease.InOutSine))
            .OnComplete(StartHandLoop);
    }

    public void PlayHandClickAnimation()
    {
        if (!CanAnimateHand())
        {
            return;
        }

        handOneShotTween?.Kill();
        StopHandLoop();
        float duration = Mathf.Max(0.05f, handClickDuration);
        Vector3 clickScale = baseIconScale * Mathf.Max(0.1f, handClickScale);
        handOneShotTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(iconTransform.DOScale(clickScale, duration).SetEase(Ease.OutSine))
            .Append(iconTransform.DOScale(baseIconScale, duration).SetEase(Ease.OutBack))
            .OnComplete(StartHandLoop);
    }

    public void PlayHandStepCompleteAnimation()
    {
        if (iconTransform == null || !iconTransform.gameObject.activeSelf)
        {
            return;
        }

        handOneShotTween?.Kill();
        StopHandLoop();
        float duration = Mathf.Max(0.05f, handStepCompleteDuration);
        Vector3 endScale = baseIconScale * Mathf.Max(0.1f, handStepCompleteScale);
        handOneShotTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(iconTransform.DOScale(endScale, duration).SetEase(Ease.InSine))
            .Append(iconTransform.DOScale(baseIconScale, duration).SetEase(Ease.OutSine));
    }

    private void PlayTextStepStartAnimation()
    {
        if (targetText == null)
        {
            return;
        }

        textStepTween?.Kill();
        float duration = Mathf.Max(0.05f, textStepStartDuration);
        Vector3 overshootScale = defaultTextScale * Mathf.Max(1f, textStepStartOvershoot);
        targetText.transform.localScale = defaultTextScale * 0.9f;
        textStepTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(targetText.transform.DOScale(overshootScale, duration * 0.55f).SetEase(Ease.OutBack))
            .Append(targetText.transform.DOScale(defaultTextScale, duration * 0.45f).SetEase(Ease.OutSine));
    }

    private void PlayTextStepEndAnimation()
    {
        if (targetText == null)
        {
            return;
        }

        StopTextPulse();
        textStepTween?.Kill();
        float duration = Mathf.Max(0.05f, textStepEndDuration);
        Vector3 endScale = defaultTextScale * Mathf.Clamp(textStepEndScale, 0.1f, 1f);
        textStepTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(targetText.transform.DOScale(endScale, duration).SetEase(Ease.InSine))
            .OnComplete(() =>
            {
                if (targetText != null)
                {
                    targetText.gameObject.SetActive(false);
                }

                ResetTextScale();
            });
    }

    private void StartTextPulse()
    {
        if (targetText == null || !isMessageVisible)
        {
            return;
        }

        textPulseTween?.Kill();
        float duration = Mathf.Max(0.2f, textPulseDuration);
        float amplitude = Mathf.Max(0.01f, textPulseAmplitude);
        Vector3 pulseScale = defaultTextScale * (1f + amplitude);
        textPulseTween = targetText.transform
            .DOScale(pulseScale, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void StopTextPulse()
    {
        textPulseTween?.Kill();
        textPulseTween = null;
    }

    private void ResetTextScale()
    {
        if (targetText != null)
        {
            targetText.transform.localScale = defaultTextScale;
        }
    }

    private void ApplyVisibility()
    {
        if (canvasGroup != null)
        {
            visibilityTween?.Kill();
            float targetAlpha = isVisible ? 1f : 0f;
            float duration = isVisible ? showDuration : hideDuration;
            Ease ease = isVisible ? showEase : hideEase;

            if (isVisible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                return;
            }

            visibilityTween = canvasGroup
                .DOFade(targetAlpha, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }
        else
        {
            gameObject.SetActive(isVisible);
        }

        ApplyIconVisibility();
    }

    private void ApplyIconVisibility()
    {
        if (iconTransform == null)
        {
            StopHandLoop();
            return;
        }

        iconFadeTween?.Kill();
        iconScaleTween?.Kill();

        bool shouldShowIcon = isVisible && isHandIconVisible;
        float targetAlpha = shouldShowIcon ? 1f : 0f;
        float fadeDuration = shouldShowIcon ? iconShowDuration : iconHideDuration;
        float scaleDuration = fadeDuration;
        Ease fadeEase = shouldShowIcon ? iconShowEase : iconHideEase;
        Ease scaleEase = fadeEase;

        GameObject iconObject = iconTransform.gameObject;
        if (shouldShowIcon && !iconObject.activeSelf)
        {
            iconObject.SetActive(true);
        }

        if (iconTransform != null)
        {
            Vector3 hiddenScale = baseIconScale * Mathf.Max(0f, iconHiddenScale);
            Vector3 targetScale = shouldShowIcon ? baseIconScale : hiddenScale;

            if (shouldShowIcon && iconTransform.localScale == Vector3.zero)
            {
                iconTransform.localScale = hiddenScale;
            }

            if (scaleDuration <= 0f)
            {
                iconTransform.localScale = targetScale;
            }
            else
            {
                iconScaleTween = iconTransform
                    .DOScale(targetScale, scaleDuration)
                    .SetEase(scaleEase)
                    .SetUpdate(true);
            }
        }

        if (shouldShowIcon)
        {
            StartHandLoop();
            return;
        }

        StopHandLoop();
        if (iconObject.activeSelf)
        {
            iconObject.SetActive(false);
        }
    }

    private void StartHandLoop()
    {
        if (iconTransform == null)
        {
            return;
        }

        StopHandLoop();
        float downDuration = Mathf.Max(0.05f, handTapDownDuration);
        float upDuration = Mathf.Max(0.05f, handTapUpDuration);
        float loopInterval = Mathf.Max(0f, handTapLoopInterval);
        Vector3 tapDownScale = baseIconScale * Mathf.Max(0.1f, handTapDownScale);
        Vector3 tapUpScale = baseIconScale * Mathf.Max(1f, handTapUpScale);

        handTapLoopSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(iconTransform.DOScale(tapDownScale, downDuration).SetEase(handTapDownEase))
            .Append(iconTransform.DOScale(tapUpScale, upDuration * 0.65f).SetEase(handTapUpEase))
            .Append(iconTransform.DOScale(baseIconScale, upDuration * 0.35f).SetEase(Ease.OutSine))
            .AppendInterval(loopInterval)
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopHandLoop()
    {
        handTapLoopSequence?.Kill();
        handTapLoopSequence = null;

        ResetIconState();
    }

    private bool CanAnimateHand()
    {
        return iconTransform != null && iconTransform.gameObject.activeSelf && isVisible && isHandIconVisible;
    }
}
