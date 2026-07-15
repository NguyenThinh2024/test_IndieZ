using Lean.Gui;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class LeanCirclePanelHole : MonoBehaviour
{
    private const string HoleShaderName = "UI/BG Logo Hole (Scaled Mask)";
    private const int CircleMaskSize = 256;

    [Header("Refs")]
    [SerializeField] private LeanCircle holeCircle = null;
    [SerializeField] private Material holeMaterialTemplate = null;
    [SerializeField] private bool autoFindCircleInChildren = true;

    [Header("Config")]
    [SerializeField, Range(0f, 0.2f)] private float holeSoftness = 0.02f;
    [SerializeField] private bool syncEveryFrame = true;

    public float HoleSoftness
    {
        get => holeSoftness;
        set
        {
            holeSoftness = Mathf.Clamp(value, 0f, 0.2f);

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(HoleSoftnessId, holeSoftness);
            }
        }
    }

    private Image panelImage;
    private Material runtimeMaterial;
    private Material originalMaterial;

    private static Texture2D circleMaskTexture;
    private static readonly int LogoTexId = Shader.PropertyToID("_LogoTex");
    private static readonly int LogoCenterId = Shader.PropertyToID("_LogoCenter");
    private static readonly int LogoScaleId = Shader.PropertyToID("_LogoScale");
    private static readonly int LogoScaleXYId = Shader.PropertyToID("_LogoScaleXY");
    private static readonly int HoleSoftnessId = Shader.PropertyToID("_HoleSoftness");

    private void Awake()
    {
        ResolveReferences();
        EnsureRuntimeMaterial();
        SyncHole();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureRuntimeMaterial();
        SyncHole();
    }

    private void LateUpdate()
    {
        if (!syncEveryFrame)
        {
            return;
        }

        SyncHole();
    }

    private void OnDisable()
    {
        RestoreOriginalMaterial();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterial();
    }

    private void OnValidate()
    {
        ResolveReferences();
        EnsureRuntimeMaterial();
        SyncHole();
    }

    [ContextMenu("Sync Hole Now")]
    public void SyncHole()
    {
        if (panelImage == null || runtimeMaterial == null || holeCircle == null)
        {
            return;
        }

        RectTransform panelRect = panelImage.rectTransform;
        RectTransform circleRect = holeCircle.rectTransform;
        Rect panelLocalRect = panelRect.rect;
        if (panelLocalRect.width <= Mathf.Epsilon || panelLocalRect.height <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 circleWorldCenter = circleRect.TransformPoint(circleRect.rect.center);
        Camera eventCamera = ResolveEventCamera(panelRect);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect,
            RectTransformUtility.WorldToScreenPoint(eventCamera, circleWorldCenter),
            eventCamera,
            out Vector2 localCenter);

        float centerU = Mathf.InverseLerp(panelLocalRect.xMin, panelLocalRect.xMax, localCenter.x);
        float centerV = Mathf.InverseLerp(panelLocalRect.yMin, panelLocalRect.yMax, localCenter.y);

        Vector3[] corners = new Vector3[4];
        circleRect.GetWorldCorners(corners);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect,
            RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]),
            eventCamera,
            out Vector2 bottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect,
            RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]),
            eventCamera,
            out Vector2 topRight);

        float scaleX = Mathf.Abs(topRight.x - bottomLeft.x) / panelLocalRect.width;
        float scaleY = Mathf.Abs(topRight.y - bottomLeft.y) / panelLocalRect.height;
        runtimeMaterial.SetVector(LogoCenterId, new Vector4(centerU, centerV, 0f, 0f));
        runtimeMaterial.SetFloat(LogoScaleId, Mathf.Max(scaleX, scaleY, 0.0001f));
        runtimeMaterial.SetVector(LogoScaleXYId, new Vector4(
            Mathf.Max(scaleX, 0.0001f),
            Mathf.Max(scaleY, 0.0001f),
            0f,
            0f));
        runtimeMaterial.SetFloat(HoleSoftnessId, holeSoftness);
    }

    private void ResolveReferences()
    {
        if (panelImage == null)
        {
            panelImage = GetComponent<Image>();
        }

        if (holeCircle == null && autoFindCircleInChildren)
        {
            holeCircle = GetComponentInChildren<LeanCircle>(true);
        }
    }

    private void EnsureRuntimeMaterial()
    {
        if (panelImage == null)
        {
            return;
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetTexture(LogoTexId, GetCircleMaskTexture());
            runtimeMaterial.SetFloat(HoleSoftnessId, holeSoftness);
            return;
        }

        Material sourceMaterial = holeMaterialTemplate;
        if (sourceMaterial == null)
        {
            Shader holeShader = Shader.Find(HoleShaderName);
            if (holeShader == null)
            {
                return;
            }

            sourceMaterial = new Material(holeShader);
        }

        originalMaterial = panelImage.material;
        runtimeMaterial = new Material(sourceMaterial)
        {
            name = sourceMaterial.name + " (Circle Hole Runtime)"
        };
        runtimeMaterial.SetTexture(LogoTexId, GetCircleMaskTexture());
        runtimeMaterial.SetVector(LogoScaleXYId, new Vector4(1f, 1f, 0f, 0f));
        runtimeMaterial.SetFloat(HoleSoftnessId, holeSoftness);
        panelImage.material = runtimeMaterial;
    }

    private void RestoreOriginalMaterial()
    {
        if (panelImage != null && runtimeMaterial != null && panelImage.material == runtimeMaterial)
        {
            panelImage.material = originalMaterial;
        }

        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }

            runtimeMaterial = null;
        }
    }

    private static Texture2D GetCircleMaskTexture()
    {
        if (circleMaskTexture != null)
        {
            return circleMaskTexture;
        }

        circleMaskTexture = new Texture2D(CircleMaskSize, CircleMaskSize, TextureFormat.Alpha8, false)
        {
            name = "LeanCirclePanelHoleMask",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float radius = (CircleMaskSize - 1) * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        Color32[] pixels = new Color32[CircleMaskSize * CircleMaskSize];

        for (int y = 0; y < CircleMaskSize; y++)
        {
            for (int x = 0; x < CircleMaskSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = distance <= radius ? (byte)255 : (byte)0;
                pixels[(y * CircleMaskSize) + x] = new Color32(255, 255, 255, alpha);
            }
        }

        circleMaskTexture.SetPixels32(pixels);
        circleMaskTexture.Apply(false, true);
        return circleMaskTexture;
    }

    private static Camera ResolveEventCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return canvas.worldCamera;
        }

        return null;
    }
}
