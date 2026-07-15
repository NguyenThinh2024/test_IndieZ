using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public static bool GameplayInputBlocked { get; private set; }
    public static bool AllowBlockedJellyTap { get; private set; }

    [Header("Panel")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PanelTap panelTap;
    [SerializeField] private GameObject extraPanel;
    [SerializeField] private bool tutorial;
    [SerializeField] private bool hideAllChildrenOnAction = true;

    [Header("Input Blocker")]
    [SerializeField] private bool blockOutsideTutorialRaycasts = true;
    [SerializeField] private Image outsideRaycastBlocker;
    [SerializeField] private Color outsideRaycastBlockerColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private bool blockTutorialPanelRaycasts = true;
    [SerializeField] private Graphic tutorialPanelRaycastTarget;

    [Header("Wave Text")]
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField, Min(0f)] private float waveAmplitude = 8f;
    [SerializeField, Min(0.1f)] private float waveFrequency = 8f;
    [SerializeField, Min(0.1f)] private float waveTravelSpeed = 4f;

    [Header("Button Pulse")]
    [SerializeField] private Button actionButton;
    [SerializeField, Min(0f)] private float pulseAmplitude = 0.08f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2.4f;

    private TMP_MeshInfo[] cachedTextMeshInfo;
    private bool hasCachedTextMesh;
    private Vector3 buttonBaseScale = Vector3.one;
    private bool tutorialPanelVisible = true;

    private void Awake()
    {
        if (panelTap == null)
        {
            panelTap = FindFirstObjectByType<PanelTap>(FindObjectsInactive.Include);
        }

        if (actionButton != null)
        {
            buttonBaseScale = actionButton.transform.localScale;
            actionButton.onClick.AddListener(HandleActionButtonClicked);
        }

        EnsureOutsideRaycastBlocker();
        EnsureTutorialPanelRaycastTarget();
    }

    private void OnEnable()
    {
        tutorialPanelVisible = true;
        CacheTextMeshData(force: true);
        SetTutorialPanelVisible(true);
        if (actionButton != null)
        {
            actionButton.transform.localScale = buttonBaseScale;
        }
    }

    private void OnDisable()
    {
        GameplayInputBlocked = false;
        AllowBlockedJellyTap = false;
        SetOutsideRaycastBlockerVisible(false);
        SetTutorialPanelRaycastTargetVisible(false);
        ResetVisualState();
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
        }
    }

    private void LateUpdate()
    {
        if (!IsPanelVisible())
        {
            return;
        }

        AnimateTextWave();
        AnimateButtonPulse();
    }

    private bool IsPanelVisible()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!tutorialPanelVisible)
        {
            return false;
        }

        if (canvasGroup != null)
        {
            return canvasGroup.alpha > 0.001f;
        }

        return true;
    }

    private void AnimateTextWave()
    {
        if (tutorialText == null)
        {
            return;
        }

        if (!hasCachedTextMesh)
        {
            CacheTextMeshData(force: true);
            if (!hasCachedTextMesh)
            {
                return;
            }
        }

        if (tutorialText.havePropertiesChanged)
        {
            CacheTextMeshData(force: true);
        }

        TMP_TextInfo textInfo = tutorialText.textInfo;
        int charCount = textInfo.characterCount;
        if (charCount <= 0)
        {
            return;
        }

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] srcVertices = cachedTextMeshInfo[materialIndex].vertices;
            Vector3[] dstVertices = textInfo.meshInfo[materialIndex].vertices;

            float phase = (Time.unscaledTime * waveTravelSpeed) - (charInfo.origin * 0.01f * waveFrequency);
            float offsetY = Mathf.Sin(phase) * waveAmplitude;
            Vector3 waveOffset = new Vector3(0f, offsetY, 0f);

            dstVertices[vertexIndex + 0] = srcVertices[vertexIndex + 0] + waveOffset;
            dstVertices[vertexIndex + 1] = srcVertices[vertexIndex + 1] + waveOffset;
            dstVertices[vertexIndex + 2] = srcVertices[vertexIndex + 2] + waveOffset;
            dstVertices[vertexIndex + 3] = srcVertices[vertexIndex + 3] + waveOffset;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            tutorialText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    private void AnimateButtonPulse()
    {
        if (actionButton == null)
        {
            return;
        }

        float scaleFactor = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * pulseAmplitude;
        actionButton.transform.localScale = buttonBaseScale * scaleFactor;
    }

    private void CacheTextMeshData(bool force)
    {
        if (tutorialText == null)
        {
            hasCachedTextMesh = false;
            return;
        }

        if (!force && hasCachedTextMesh)
        {
            return;
        }

        tutorialText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tutorialText.textInfo;
        if (textInfo == null || textInfo.meshInfo == null || textInfo.meshInfo.Length == 0)
        {
            hasCachedTextMesh = false;
            return;
        }

        cachedTextMeshInfo = textInfo.CopyMeshInfoVertexData();
        hasCachedTextMesh = true;
    }

    private void ResetVisualState()
    {
        if (actionButton != null)
        {
            actionButton.transform.localScale = buttonBaseScale;
        }

        if (tutorialText != null)
        {
            tutorialText.ForceMeshUpdate();
        }
    }

    private void HandleActionButtonClicked()
    {
        SetTutorialPanelVisible(false);

        if (extraPanel != null)
        {
            extraPanel.SetActive(true);
        }
    }

    private void SetTutorialPanelVisible(bool visible)
    {
        tutorialPanelVisible = visible;
        GameplayInputBlocked = visible;
        AllowBlockedJellyTap = false;

        if (hideAllChildrenOnAction)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (extraPanel != null && child.gameObject == extraPanel)
                {
                    continue;
                }

                child.gameObject.SetActive(visible);
            }
        }
        else
        {
            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(visible);
            }

            if (tutorialText != null)
            {
                tutorialText.gameObject.SetActive(visible);
            }
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        SetOutsideRaycastBlockerVisible(visible);
        SetTutorialPanelRaycastTargetVisible(visible);
    }

    private void EnsureOutsideRaycastBlocker()
    {
        if (!blockOutsideTutorialRaycasts || outsideRaycastBlocker != null)
        {
            return;
        }

        Transform parent = transform.parent != null ? transform.parent : transform;
        GameObject blockerObject = new GameObject($"{nameof(Tutorial)}_OutsideRaycastBlocker", typeof(RectTransform), typeof(Image));
        blockerObject.transform.SetParent(parent, false);

        RectTransform rectTransform = blockerObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        outsideRaycastBlocker = blockerObject.GetComponent<Image>();
        outsideRaycastBlocker.color = outsideRaycastBlockerColor;
        outsideRaycastBlocker.raycastTarget = true;

        if (transform.parent != null)
        {
            blockerObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
            transform.SetAsLastSibling();
        }

        blockerObject.SetActive(false);
    }

    private void SetOutsideRaycastBlockerVisible(bool visible)
    {
        if (!blockOutsideTutorialRaycasts)
        {
            return;
        }

        EnsureOutsideRaycastBlocker();
        if (outsideRaycastBlocker == null)
        {
            return;
        }

        if (outsideRaycastBlocker.transform.parent == transform.parent && transform.parent != null)
        {
            outsideRaycastBlocker.transform.SetSiblingIndex(transform.GetSiblingIndex());
            transform.SetAsLastSibling();
        }

        outsideRaycastBlocker.color = outsideRaycastBlockerColor;
        outsideRaycastBlocker.raycastTarget = visible;
        outsideRaycastBlocker.gameObject.SetActive(visible);
    }

    private void EnsureTutorialPanelRaycastTarget()
    {
        if (!blockTutorialPanelRaycasts || tutorialPanelRaycastTarget != null)
        {
            return;
        }

        tutorialPanelRaycastTarget = GetComponent<Graphic>();
        if (tutorialPanelRaycastTarget != null)
        {
            return;
        }

        Image image = gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        tutorialPanelRaycastTarget = image;
    }

    private void SetTutorialPanelRaycastTargetVisible(bool visible)
    {
        if (!blockTutorialPanelRaycasts)
        {
            return;
        }

        EnsureTutorialPanelRaycastTarget();
        if (tutorialPanelRaycastTarget == null)
        {
            return;
        }

        tutorialPanelRaycastTarget.raycastTarget = visible;
    }
}
