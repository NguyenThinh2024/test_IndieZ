using UnityEngine;
using UnityEngine.UI;

public class UICursorFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private Canvas canvas;

    [Header("Scale")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 pressedScale = new Vector3(1.15f, 1.15f, 1.15f);
    [SerializeField] private float scaleSpeed = 18f;

    private Vector3 targetScale;

    private void OnEnable()
    {
        Cursor.visible = false;

        if (cursorRect != null)
        {
            cursorRect.localScale = normalScale;
            targetScale = normalScale;
        }
    }

    private void Update()
    {
        if (cursorRect == null || canvas == null) return;

        UpdateCursorPosition();
        UpdateCursorScale();
    }

    private void UpdateCursorPosition()
    {
        Vector2 pos;
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out pos
        );

        cursorRect.anchoredPosition = pos;
    }

    private void UpdateCursorScale()
    {
        if (Input.GetMouseButtonDown(0))
        {
            targetScale = pressedScale;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            targetScale = normalScale;
        }

        cursorRect.localScale = Vector3.Lerp(
            cursorRect.localScale,
            targetScale,
            scaleSpeed * Time.unscaledDeltaTime
        );
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}