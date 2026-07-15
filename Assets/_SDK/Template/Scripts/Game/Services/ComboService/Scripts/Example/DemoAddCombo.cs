using Nexzap.Base.Gameplay;
using UnityEngine;

/// <summary>
/// Demo: click/tap ANYWHERE => tăng combo + spawn sao bay tới icon sao trong UICombo.
/// </summary>
public class DemoAddCombo : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("Z distance dùng khi convert screen->world. Với camera 2D orthographic có thể để 0 hoặc 10.")]
    [SerializeField] private float screenToWorldZ = 10f;

    [Tooltip("Bonus sao thêm cho mỗi hit (ngoài base + combo multiplier)")]
    [SerializeField] private int extraStarBonus = 0;

    private ComboService _combo;
    private Camera _cam;

    private void Start()
    {
        _combo = GameController.Instance.Services.Get<ComboService>();
        _cam = Camera.main;

        if (_combo == null)
            Debug.LogError("[DemoAddCombo] ComboService not found in GameplayController.Services");

        if (_cam == null)
            Debug.LogWarning("[DemoAddCombo] Camera.main is null. UIStarFlyEmitter WorldToScreen may fail.");
    }

    private void Update()
    {
        if (_combo == null) return;

        // PC
        if (Input.GetMouseButtonDown(0))
        {
            TriggerAtScreen(Input.mousePosition);
            return;
        }
    }

    private void TriggerAtScreen(Vector2 screenPos)
    {
        Vector3 flyFromWorld;

        if (_cam != null)
        {
            var sp = new Vector3(screenPos.x, screenPos.y, screenToWorldZ);
            flyFromWorld = _cam.ScreenToWorldPoint(sp);
        }
        else
        {
            // fallback: dùng screen pos như world
            flyFromWorld = new Vector3(screenPos.x, screenPos.y, 0f);
        }

        _combo.NotifyComboHit(
            flyFrom: flyFromWorld,
            extraStarBonus: extraStarBonus
        );
    }
}