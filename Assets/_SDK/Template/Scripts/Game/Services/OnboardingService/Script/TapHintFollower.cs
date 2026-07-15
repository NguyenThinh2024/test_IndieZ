using UnityEngine;

public sealed class TapHintFollower : MonoBehaviour
{
    [SerializeField] private RectTransform hintRect = null;
    [SerializeField] private Vector2 canvasOffset = Vector2.zero;

    private bool hasCanvasPoint;
    private Vector2 currentCanvasPoint;

    private void Awake()
    {
        if (hintRect == null)
        {
            hintRect = transform as RectTransform;
        }

        SetHintVisible(hasCanvasPoint);
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    public void AttachToCanvasPoint(Vector2 canvasPoint)
    {
        currentCanvasPoint = canvasPoint;
        hasCanvasPoint = true;
        SetHintVisible(true);
        UpdatePosition();
    }

    public void Detach()
    {
        hasCanvasPoint = false;
        SetHintVisible(false);
    }

    private void UpdatePosition()
    {
        if (!hasCanvasPoint || hintRect == null)
        {
            return;
        }

        RectTransform parentRect = hintRect.parent as RectTransform;
        if (parentRect == null)
        {
            hintRect.localPosition = new Vector3(
                currentCanvasPoint.x + canvasOffset.x,
                currentCanvasPoint.y + canvasOffset.y,
                hintRect.localPosition.z);
            return;
        }

        hintRect.anchoredPosition = currentCanvasPoint + canvasOffset;
    }

    private void SetHintVisible(bool visible)
    {
        if (hintRect != null && hintRect.gameObject.activeSelf != visible)
        {
            hintRect.gameObject.SetActive(visible);
        }
    }
}
