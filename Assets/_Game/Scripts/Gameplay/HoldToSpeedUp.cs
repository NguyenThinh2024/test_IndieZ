using UnityEngine;

public class HoldToSpeedUp : MonoBehaviour
{
    private const float DefaultTimeScale = 1f;

    [SerializeField] private float holdDelay = 0.2f;
    [SerializeField] private float holdTimeScale = 4f;

    private float currentHoldTime;

    private void Update()
    {
        UpdateHoldTime();
        SetTimeScale(ShouldSpeedUp() ? holdTimeScale : DefaultTimeScale);
    }

    private void OnDisable()
    {
        ResetHoldState();
    }

    private void OnDestroy()
    {
        ResetHoldState();
    }

    private void UpdateHoldTime()
    {
        if (!IsHoldingScreen())
        {
            currentHoldTime = 0f;
            return;
        }

        currentHoldTime += Time.unscaledDeltaTime;
    }

    private bool ShouldSpeedUp()
    {
        return currentHoldTime >= holdDelay;
    }

    private void ResetHoldState()
    {
        currentHoldTime = 0f;
        SetTimeScale(DefaultTimeScale);
    }

    private static bool IsHoldingScreen()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
        }

        return Input.GetMouseButton(0);
    }

    private static void SetTimeScale(float value)
    {
        if (Mathf.Approximately(Time.timeScale, value))
        {
            return;
        }

        Time.timeScale = value;
    }
}
