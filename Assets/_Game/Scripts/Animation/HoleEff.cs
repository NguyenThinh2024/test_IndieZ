using Thinh.Base.UI;
using UnityEngine;

public class HoleEff : MonoBehaviour
{
    [SerializeField] private GameObject hideTarget = null;

    [SerializeField] private bool speedUpGameplayWhilePressed = true;
    [SerializeField, Min(0.01f)] private float pressedTimeScale = 2f;
    [SerializeField] private bool useRectInputFallback = true;

    private bool isPressed;
    private bool hasStoredTimeScale;
    private float storedTimeScale = 1f;
    private float storedFixedDeltaTime = 0.02f;
    private RectTransform cachedRectTransform;
    private UIBasePopup ownerPopup;
    private bool isHiddenByOtherPopup;

    private void Awake()
    {
        ResolveHideTarget();
        ownerPopup = GetComponentInParent<UIBasePopup>(true);
        cachedRectTransform = transform as RectTransform;
    }

    private void OnEnable()
    {
        isPressed = false;
        isHiddenByOtherPopup = false;
        RefreshHiddenState();
    }

    private void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
    }

    private void OnValidate()
    {
        ResolveHideTarget();
    }

    private void Update()
    {
        TickRectInputFallback();
        TickPopupVisibility();
    }

    public void Press()
    {
        SetPressed(true);
    }

    public void Release()
    {
        SetPressed(false);
    }

    private void ResolveHideTarget()
    {
        if (hideTarget != null)
        {
            return;
        }

        Transform selfTransform = transform;
        hideTarget = selfTransform.childCount > 0 ? selfTransform.GetChild(0).gameObject : gameObject;
    }

    private void SetPressed(bool pressed)
    {
        if (isPressed == pressed)
        {
            return;
        }

        isPressed = pressed;
        RefreshHiddenState();
        if (pressed)
        {
            ApplyPressedTimeScale();
        }
        else
        {
            RestoreTimeScaleIfNeeded();
        }
    }

    private void TickPopupVisibility()
    {
        bool shouldHideByPopup = false;
        if (UIPopupController.TryGetInstance(out UIPopupController popupController))
        {
            shouldHideByPopup = popupController.AnyPopupActiveExcept(ownerPopup);
        }

        if (isHiddenByOtherPopup == shouldHideByPopup)
        {
            return;
        }

        isHiddenByOtherPopup = shouldHideByPopup;
        RefreshHiddenState();
    }

    private void RefreshHiddenState()
    {
        SetHiddenInPanel(isPressed || isHiddenByOtherPopup);
    }

    private void SetHiddenInPanel(bool hidden)
    {
        if (hideTarget != null)
        {
            hideTarget.SetActive(!hidden);
        }
    }

    private void TickRectInputFallback()
    {
        if (Tutorial.GameplayInputBlocked)
        {
            SetPressed(false);
            return;
        }

        if (!useRectInputFallback || cachedRectTransform == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && IsScreenPointInside(Input.mousePosition))
        {
            SetPressed(true);
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            SetPressed(false);
            return;
        }

        if (isPressed && !Input.GetMouseButton(0))
        {
            SetPressed(false);
        }
    }

    private bool IsScreenPointInside(Vector2 screenPoint)
    {
        Camera eventCamera = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(cachedRectTransform, screenPoint, eventCamera);
    }

    private void ApplyPressedTimeScale()
    {
        if (!speedUpGameplayWhilePressed)
        {
            return;
        }

        if (!hasStoredTimeScale)
        {
            storedTimeScale = Time.timeScale;
            storedFixedDeltaTime = Time.fixedDeltaTime;
            hasStoredTimeScale = true;
        }

        Time.timeScale = pressedTimeScale;
        Time.fixedDeltaTime = storedFixedDeltaTime * pressedTimeScale;
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!hasStoredTimeScale)
        {
            return;
        }

        Time.timeScale = storedTimeScale;
        Time.fixedDeltaTime = storedFixedDeltaTime;
        hasStoredTimeScale = false;
    }
}
