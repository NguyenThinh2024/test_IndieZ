using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelTap : MonoBehaviour
{
    public struct ViewData
    {
        public string Message;
        public bool ShowMessage;
        public bool ShowActionButton;
        public string ActionButtonText;
        public bool ActionButtonInteractable;
    }

    [FoldoutGroup("View"), SerializeField] private CanvasGroup canvasGroup;

    [FoldoutGroup("Text"), SerializeField] private TMP_Text targetText;
    [FoldoutGroup("Text"), SerializeField] private GameObject messageRoot;
    [FoldoutGroup("Text"), SerializeField] private bool autoFindText = true;

    [FoldoutGroup("Button"), SerializeField] private Button actionButton;
    [FoldoutGroup("Button"), SerializeField] private GameObject actionButtonRoot;
    [FoldoutGroup("Button"), SerializeField] private TMP_Text actionButtonText;

    [FoldoutGroup("Icon"), SerializeField] private GameObject iconRoot;
    [FoldoutGroup("Icon"), SerializeField] private CanvasGroup iconCanvasGroup;
    [FoldoutGroup("Icon"), SerializeField] private Transform iconTransform;

    [FoldoutGroup("Animation"), SerializeField, Min(1f)] private float breatheScale = 1.08f;
    [FoldoutGroup("Animation"), SerializeField, Min(0.1f)] private float breatheSpeed = 2.2f;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float showDuration = 0.2f;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float hideDuration = 0.15f;
    [FoldoutGroup("Animation"), SerializeField] private Ease showEase = Ease.OutSine;
    [FoldoutGroup("Animation"), SerializeField] private Ease hideEase = Ease.InSine;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float iconShowDuration = 0.2f;
    [FoldoutGroup("Animation"), SerializeField, Min(0f)] private float iconHideDuration = 0.15f;
    [FoldoutGroup("Animation"), SerializeField] private Ease iconShowEase = Ease.OutBack;
    [FoldoutGroup("Animation"), SerializeField] private Ease iconHideEase = Ease.InBack;
    [FoldoutGroup("Animation"), SerializeField] private float iconHiddenScale = 0.85f;

    private Vector3 baseTextScale = Vector3.one;
    private Vector3 baseIconScale = Vector3.one;
    private bool isVisible = true;
    private string defaultActionButtonText = "Next";
    private Tween visibilityTween;
    private Tween iconFadeTween;
    private Tween iconScaleTween;

    public event Action ActionButtonClicked;

    private void Awake()
    {
        ResolveCanvasGroup();
        ResolveText();
        ResolveRoots();
        ResolveIconRefs();
        ResolveActionButtonText();
        CacheBaseScale();
        CacheDefaultActionButtonText();
        RegisterButton();
    }

    private void OnEnable()
    {
        ResolveCanvasGroup();
        ResolveText();
        ResolveRoots();
        ResolveIconRefs();
        ResolveActionButtonText();
        CacheBaseScale();
        CacheDefaultActionButtonText();
        ApplyVisibility();
        ApplyBreathingScale(1f);
    }

    private void OnDisable()
    {
        visibilityTween?.Kill();
        iconFadeTween?.Kill();
        iconScaleTween?.Kill();
        ResetScale();
    }

    private void OnValidate()
    {
        ResolveCanvasGroup();
        ResolveText();
        ResolveRoots();
        ResolveIconRefs();
        ResolveActionButtonText();
        CacheBaseScale();
        CacheDefaultActionButtonText();
    }

    private void Update()
    {
        if (targetText == null)
        {
            return;
        }

        float wave = (Mathf.Sin(Time.unscaledTime * breatheSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scaleMultiplier = Mathf.Lerp(1f, breatheScale, wave);
        ApplyBreathingScale(scaleMultiplier);
    }

    private void ResolveText()
    {
        if (targetText == null && autoFindText)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text candidate = texts[i];
                if (candidate == null)
                {
                    continue;
                }

                if (actionButton != null && candidate.transform.IsChildOf(actionButton.transform))
                {
                    continue;
                }

                targetText = candidate;
                break;
            }
        }
    }

    private void ResolveRoots()
    {
        if (messageRoot == null && targetText != null)
        {
            messageRoot = targetText.gameObject;
        }

        if (actionButtonRoot == null && actionButton != null)
        {
            actionButtonRoot = actionButton.gameObject;
        }
    }

    private void ResolveActionButtonText()
    {
        if (actionButtonText == null && actionButton != null)
        {
            actionButtonText = actionButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void ResolveIconRefs()
    {
        if (iconRoot == null && iconTransform != null)
        {
            iconRoot = iconTransform.gameObject;
        }

        if (iconTransform == null && iconRoot != null)
        {
            iconTransform = iconRoot.transform;
        }

        if (iconCanvasGroup == null && iconRoot != null)
        {
            iconCanvasGroup = iconRoot.GetComponent<CanvasGroup>();
        }
    }

    private void ResolveCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void CacheBaseScale()
    {
        if (targetText != null)
        {
            baseTextScale = targetText.transform.localScale;
        }

        if (iconTransform != null)
        {
            baseIconScale = iconTransform.localScale;
        }
    }

    private void CacheDefaultActionButtonText()
    {
        if (actionButtonText != null && !string.IsNullOrWhiteSpace(actionButtonText.text))
        {
            defaultActionButtonText = actionButtonText.text;
        }
    }

    private void RegisterButton()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
            actionButton.onClick.AddListener(HandleActionButtonClicked);
        }
    }

    private void ApplyBreathingScale(float scaleMultiplier)
    {
        if (targetText != null)
        {
            targetText.transform.localScale = baseTextScale * scaleMultiplier;
        }
    }

    private void ResetScale()
    {
        if (targetText != null)
        {
            targetText.transform.localScale = baseTextScale;
        }

        if (iconTransform != null)
        {
            iconTransform.localScale = baseIconScale;
        }
    }

    private void OnDestroy()
    {
        visibilityTween?.Kill();
        iconFadeTween?.Kill();
        iconScaleTween?.Kill();
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
        }
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ApplyVisibility();
    }

    public void Render(ViewData viewData)
    {
        SetMessage(viewData.Message, viewData.ShowMessage);
        SetActionButton(viewData.ShowActionButton, viewData.ActionButtonText, viewData.ActionButtonInteractable);
    }

    public void SetMessage(string message, bool visible)
    {
        if (targetText != null)
        {
            targetText.text = message ?? string.Empty;
        }

        if (messageRoot != null)
        {
            bool shouldShow = visible && !string.IsNullOrWhiteSpace(message);
            messageRoot.SetActive(shouldShow);
        }
    }

    public void SetActionButton(bool visible, string buttonText, bool interactable = true)
    {
        if (actionButtonText != null)
        {
            actionButtonText.text = string.IsNullOrWhiteSpace(buttonText)
                ? defaultActionButtonText
                : buttonText;
        }

        if (actionButton != null)
        {
            actionButton.interactable = visible && interactable;
        }

        if (actionButtonRoot != null)
        {
            actionButtonRoot.SetActive(visible);
        }
    }

    private void HandleActionButtonClicked()
    {
        ActionButtonClicked?.Invoke();
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
        if (iconRoot == null)
        {
            return;
        }

        iconFadeTween?.Kill();
        iconScaleTween?.Kill();

        float targetAlpha = isVisible ? 1f : 0f;
        float fadeDuration = isVisible ? iconShowDuration : iconHideDuration;
        float scaleDuration = fadeDuration;
        Ease fadeEase = isVisible ? iconShowEase : iconHideEase;
        Ease scaleEase = fadeEase;

        if (isVisible && !iconRoot.activeSelf)
        {
            iconRoot.SetActive(true);
        }

        if (iconCanvasGroup != null)
        {
            iconCanvasGroup.interactable = false;
            iconCanvasGroup.blocksRaycasts = false;

            if (fadeDuration <= 0f)
            {
                iconCanvasGroup.alpha = targetAlpha;
            }
            else
            {
                iconFadeTween = iconCanvasGroup
                    .DOFade(targetAlpha, fadeDuration)
                    .SetEase(fadeEase)
                    .SetUpdate(true);
            }
        }

        if (iconTransform != null)
        {
            Vector3 hiddenScale = baseIconScale * Mathf.Max(0f, iconHiddenScale);
            Vector3 targetScale = isVisible ? baseIconScale : hiddenScale;

            if (isVisible && iconTransform.localScale == Vector3.zero)
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
    }
}
