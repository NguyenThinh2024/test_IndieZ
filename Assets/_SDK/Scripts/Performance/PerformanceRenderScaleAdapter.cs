using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PerformanceRenderScaleAdapter : MonoBehaviour
{
    [Header("RAM Threshold")]
    [SerializeField] private int lowRamMB = 4096;

    [Header("Render Scale")]
    [SerializeField] private float lowRamRenderScale = 0.85f;
    [SerializeField] private float normalRenderScale = 1.0f;

    [Header("Apply")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool logDebug = true;

    private UniversalRenderPipelineAsset urpAsset;

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyRenderScaleByRam();
        }
    }

    public void ApplyRenderScaleByRam()
    {
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urpAsset == null)
        {
            DebugLog.LogWarning("[PerformanceAdapter] Current Render Pipeline is not URP Asset.");
            return;
        }

        int systemRam = SystemInfo.systemMemorySize;

        bool isLowRamDevice = systemRam <= lowRamMB;

        float targetRenderScale = isLowRamDevice
            ? lowRamRenderScale
            : normalRenderScale;

        urpAsset.renderScale = targetRenderScale;

        if (logDebug)
        {
            DebugLog.Log(
                $"[PerformanceAdapter] RAM: {systemRam}MB | " +
                $"Low RAM: {isLowRamDevice} | " +
                $"Render Scale: {targetRenderScale}"
            );
        }
    }
}