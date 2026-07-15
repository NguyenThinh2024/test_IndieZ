using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SliderLinearValueHandler : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Transform backgroundTransform;
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private Graphic backgroundGraphic;
    [SerializeField, Range(0f, 1f)] private float value;
    [SerializeField, Min(0f)] private float duration = 0.125f;
    [SerializeField] private bool setSliderWithoutNotify = true;
    [SerializeField] private Color valueZeroColor = new Color32(0xB9, 0xB8, 0x20, 0xFF);
    [SerializeField] private Color valueOneColor = new Color32(0x8B, 0x9A, 0x88, 0xFF);

    private Tween valueTween;

    public float Value => value;

    private void Awake()
    {
        ResolveReferences();
        ApplyValue(value);
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyValue(value);
    }

    private void OnDisable()
    {
        valueTween?.Kill();
        valueTween = null;
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyValue(value);
    }

    public void Show()
    {
        SetValue(1f);
    }

    public void Hide()
    {
        SetValue(0f);
    }

    public void SetValue(float targetValue)
    {
        SetValue(targetValue, duration);
    }

    public void SetValue(float targetValue, float tweenDuration)
    {
        ResolveReferences();

        targetValue = Mathf.Clamp01(targetValue);
        valueTween?.Kill();

        if (tweenDuration <= 0f)
        {
            ApplyValue(targetValue);
            return;
        }

        valueTween = DOTween
            .To(GetValue, ApplyValue, targetValue, tweenDuration)
            .SetEase(Ease.Linear)
            .SetTarget(this)
            .OnComplete(() => valueTween = null);
    }

    public void SetValueInstant(float targetValue)
    {
        ResolveReferences();
        valueTween?.Kill();
        valueTween = null;
        ApplyValue(Mathf.Clamp01(targetValue));
    }

    private float GetValue()
    {
        return value;
    }

    private void ApplyValue(float newValue)
    {
        value = Mathf.Clamp01(newValue);
        ApplySliderValue(value);
        ApplyBackgroundColor(value);
    }

    private void ApplySliderValue(float targetValue)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        if (setSliderWithoutNotify)
        {
            slider.SetValueWithoutNotify(targetValue);
            return;
        }

        slider.value = targetValue;
    }

    private void ApplyBackgroundColor(float targetValue)
    {
        if (backgroundGraphic == null)
        {
            return;
        }

        Color targetColor = Color.Lerp(valueZeroColor, valueOneColor, targetValue);
        Color currentColor = backgroundGraphic.color;
        targetColor.a = currentColor.a;
        backgroundGraphic.color = targetColor;
    }

    private void ResolveReferences()
    {
        if (slider == null)
        {
            TryGetComponent(out slider);
        }

        if (backgroundTransform == null && backgroundCanvasGroup != null)
        {
            backgroundTransform = backgroundCanvasGroup.transform;
        }

        if (backgroundTransform == null && backgroundGraphic != null)
        {
            backgroundTransform = backgroundGraphic.transform;
        }

        if (backgroundTransform == null)
        {
            return;
        }

        if (backgroundCanvasGroup == null)
        {
            backgroundTransform.TryGetComponent(out backgroundCanvasGroup);
        }

        if (backgroundGraphic == null)
        {
            backgroundTransform.TryGetComponent(out backgroundGraphic);
        }
    }
}
