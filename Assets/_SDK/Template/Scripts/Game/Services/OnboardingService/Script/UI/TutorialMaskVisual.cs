using Lean.Gui;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMaskVisual : MonoBehaviour
{
    [FoldoutGroup("Overlay"), SerializeField] private Image rootImage = null;
    [FoldoutGroup("Overlay"), SerializeField] private bool blockRaycasts = true;
    [FoldoutGroup("Overlay"), SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.72f);
    [FoldoutGroup("Overlay"), SerializeField] private Color frameColor = new Color(1f, 1f, 1f, 0.95f);
    [FoldoutGroup("Overlay"), SerializeField, Min(0f)] private float frameThickness = 4f;
    [FoldoutGroup("Overlay"), SerializeField, Range(0f, 0.2f)] private float holeSoftness = 0.02f;
    [FoldoutGroup("Overlay"), SerializeField] private LeanCircle focusCircle = null;
    [FoldoutGroup("Overlay"), SerializeField] private LeanCirclePanelHole circleHolePanel = null;
    [FoldoutGroup("Overlay"), SerializeField] private TapHintFollower tapHintFollower = null;
    [FoldoutGroup("Overlay"), SerializeField] private bool debugShowSolidFocusFrame = false;
    [FoldoutGroup("Overlay"), SerializeField] private Color debugFocusFrameColor = new Color(1f, 0f, 0f, 0.35f);

    public RectTransform FocusCircleRectTransform => focusCircle != null ? focusCircle.rectTransform : null;

    private void Awake()
    {
        ApplyRootOverlaySettings();
    }

    private void OnValidate()
    {
        ApplyRootOverlaySettings();
        ApplyStyle();
    }

    public void ApplyStyle()
    {
        bool useCircleHoleOverlay = focusCircle != null && circleHolePanel != null;
        ApplyRootOverlaySettings();

        if (focusCircle != null)
        {
            focusCircle.color = debugShowSolidFocusFrame ? debugFocusFrameColor : frameColor;
            focusCircle.Thickness = debugShowSolidFocusFrame ? -1f : frameThickness;
            focusCircle.raycastTarget = false;
        }

        if (circleHolePanel != null)
        {
            circleHolePanel.enabled = useCircleHoleOverlay;
            if (useCircleHoleOverlay)
            {
                circleHolePanel.HoleSoftness = holeSoftness;
                circleHolePanel.SyncHole();
            }
        }
    }

    public void ApplyFocusRect(RectTransform canvasRoot, Rect focusRect)
    {
        if (canvasRoot == null)
        {
            return;
        }

        Vector2 canvasCenter = canvasRoot.rect.center;

        if (focusCircle != null)
        {
            RectTransform circleRect = focusCircle.rectTransform;
            float diameter = Mathf.Max(focusRect.width, focusRect.height);

            circleRect.anchorMin = new Vector2(0.5f, 0.5f);
            circleRect.anchorMax = new Vector2(0.5f, 0.5f);
            circleRect.pivot = new Vector2(0.5f, 0.5f);
            circleRect.anchoredPosition = focusRect.center - canvasCenter;
            circleRect.sizeDelta = new Vector2(diameter, diameter);
        }

        if (tapHintFollower != null)
        {
            tapHintFollower.AttachToCanvasPoint(focusRect.center - canvasCenter);
        }
    }

    public void SetVisualsActive(bool visible)
    {
        if (focusCircle != null)
        {
            focusCircle.rectTransform.gameObject.SetActive(visible);
        }

        if (tapHintFollower != null)
        {
            if (visible)
            {
                // Position will be refreshed by ApplyFocusRect on the next mask update.
            }
            else
            {
                tapHintFollower.Detach();
            }
        }
    }

    private void ApplyRootOverlaySettings()
    {
        if (rootImage == null)
        {
            return;
        }

        bool useCircleHoleOverlay = focusCircle != null && circleHolePanel != null;
        rootImage.color = useCircleHoleOverlay ? overlayColor : Color.clear;
        rootImage.raycastTarget = blockRaycasts;
    }
}
