#if IAP
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Thinh.Base.IAP;
using Cysharp.Threading.Tasks;

namespace Thinh.Base.IAP
{
/// <summary>
/// IAP shop: chứa các IAPButtonUI và hỗ trợ scroll tới product tương ứng.
/// </summary>
public class IAPShopUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private ScrollRect scrollRect;          // ScrollRect của list IAP
    [SerializeField] private List<IAPButtonUI> iapButtons;   // Tất cả IAP button trong shop (có thể auto-fill)
        [SerializeField] private LayoutGroup layoutGroup;

    [Header("Config")]
    [SerializeField] private bool isHorizontal = true;       // True = scroll ngang, False = scroll dọc
    [SerializeField] private float scrollDuration = 0.25f;   // Thời gian animate scroll

    private Coroutine scrollRoutine;

    private void Start()
    {
        EnsureButtonCache();
        ReactiveLayout().Forget();
    }

    private async UniTask ReactiveLayout()
    {
        try
        {
            await UniTask.DelayFrame(1);
            layoutGroup.enabled = false;
            await UniTask.DelayFrame(1);
            layoutGroup.enabled = true;
        }
        catch (System.Exception ex) { }
    }


#if UNITY_EDITOR
        /// <summary>
        /// Trong Editor: mỗi lần thay đổi trên Inspector sẽ tự động scan các con và fill lại list iapButtons.
        /// Giúp đỡ phải kéo tay từng button.
        /// </summary>
        private void OnValidate()
    {
        EnsureButtonCache();
    }
#endif

        /// <summary>
        /// Scroll tới IAP product theo IAPProductType (enum).
        /// Ví dụ: IAPProductType.CoinPack1, Bundle1, ...
        /// </summary>
        public void ScrollToProduct(IAPProductType productType)
        {
            EnsureButtonCache();

            if (scrollRect == null || iapButtons == null || iapButtons.Count == 0)
                    return;

            // Tìm button có productType trùng
            int index = iapButtons.FindIndex(b => b != null && b.productType == productType);
            if (index < 0)
            {
                Debug.LogWarning($"[IAPShopUI] Không tìm thấy IAPButtonUI cho product: {productType}");
                return;
            }

            ScrollToIndex(index);
        }

    /// <summary>
    /// Scroll tới product theo storeId string (ví dụ: "coin_pack_1").
    /// Dùng map từ IAPIdMap.
    /// </summary>
    public void ScrollToProductId(string storeId)
    {
        if (string.IsNullOrEmpty(storeId)) return;

        EnsureButtonCache();

        // Map string -> enum rồi dùng hàm trên
        foreach (IAPProductType t in System.Enum.GetValues(typeof(IAPProductType)))
        {
            string id = IAPIdMap.GetID(t);
            if (!string.IsNullOrEmpty(id) && id == storeId)
            {
                ScrollToProduct(t);
                return;
            }
        }

        Debug.LogWarning($"[IAPShopUI] Không tìm thấy IAPProductType tương ứng storeId: {storeId}");
    }

    /// <summary>
    /// Scroll theo index trong list (giả định các item chia đều).
    /// </summary>
    private void ScrollToIndex(int index)
    {
        if (scrollRect == null || scrollRect.content == null)
            return;

        int count = iapButtons.Count;
        if (count == 0)
            return;

        index = Mathf.Clamp(index, 0, count - 1);

        var targetButton = iapButtons[index];
        if (targetButton == null) return;

        var targetRect = targetButton.GetComponent<RectTransform>();
        if (targetRect == null) return;

        if (scrollRoutine != null)
            StopCoroutine(scrollRoutine);

        scrollRoutine = StartCoroutine(AnimateScrollTo(targetRect));
    }

    private System.Collections.IEnumerator AnimateScrollTo(RectTransform target)
    {
        if (scrollRect == null || scrollRect.content == null)
            yield break;

            yield return new WaitForSeconds(0.5f); //đợi 0.5s để scroll tới

            // Đảm bảo layout được rebuild trước khi tính toán
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        RectTransform viewport = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.transform as RectTransform;

        if (viewport == null)
            yield break;

        float targetNormalized = isHorizontal
            ? CalculateHorizontalNormalized(target, viewport)
            : CalculateVerticalNormalized(target, viewport);

        if (float.IsNaN(targetNormalized))
            yield break;

        float start = isHorizontal
            ? scrollRect.horizontalNormalizedPosition
            : scrollRect.verticalNormalizedPosition;

        float elapsed = 0f;
        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float lerp = scrollDuration > 0f ? Mathf.Clamp01(elapsed / scrollDuration) : 1f;
            float value = Mathf.Lerp(start, targetNormalized, lerp);

            if (isHorizontal)
                scrollRect.horizontalNormalizedPosition = value;
            else
                scrollRect.verticalNormalizedPosition = value;

            yield return null;
        }

        if (isHorizontal)
            scrollRect.horizontalNormalizedPosition = targetNormalized;
        else
            scrollRect.verticalNormalizedPosition = targetNormalized;

        scrollRoutine = null;
    }

    private float CalculateHorizontalNormalized(RectTransform target, RectTransform viewport)
    {
        var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.content, scrollRect.content);
        var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.content, target);

        float scrollableWidth = contentBounds.size.x - viewport.rect.width;
        if (scrollableWidth <= 0f)
            return 0f;

        float leftLimit = contentBounds.min.x + viewport.rect.width * 0.5f;
        float rightLimit = contentBounds.max.x - viewport.rect.width * 0.5f;
        if (rightLimit - leftLimit <= 0f)
            return 0f;

        float clampedCenter = Mathf.Clamp(targetBounds.center.x, leftLimit, rightLimit);
        float normalized = (clampedCenter - leftLimit) / (rightLimit - leftLimit);
        return Mathf.Clamp01(normalized);
    }

    private float CalculateVerticalNormalized(RectTransform target, RectTransform viewport)
    {
        var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.content, scrollRect.content);
        var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.content, target);

        float scrollableHeight = contentBounds.size.y - viewport.rect.height;
        if (scrollableHeight <= 0f)
            return 1f;

        float bottomLimit = contentBounds.min.y + viewport.rect.height * 0.5f;
        float topLimit = contentBounds.max.y - viewport.rect.height * 0.5f;
        if (topLimit - bottomLimit <= 0f)
            return 1f;

        float clampedCenter = Mathf.Clamp(targetBounds.center.y, bottomLimit, topLimit);
        float normalized = (clampedCenter - bottomLimit) / (topLimit - bottomLimit);
        return Mathf.Clamp01(normalized);
    }

    private void EnsureButtonCache()
    {
        if (iapButtons == null || iapButtons.Count == 0)
        {
            iapButtons = GetComponentsInChildren<IAPButtonUI>(includeInactive: true).ToList();
        }
    }
}
}
#endif