using System;
using System.Collections;
using UnityEngine;

public sealed class UICoinFlyItem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Burst")]
    [SerializeField] private float burstPunchScale = 0.12f;

    [Header("Fly Shape (More Random)")]
    [SerializeField] private float startBendFactor = 0.55f;
    [SerializeField] private float bendRandomPx = 80f;

    [Header("Fly Noise")]
    [SerializeField] private float wobbleFreqMin = 6f;
    [SerializeField] private float wobbleFreqMax = 12f;

    [Header("Shrink While Flying")]
    [Tooltip("Scale cuối khi coin chạm target (0.4~0.8 nhìn đẹp).")]
    [SerializeField] private float endScale = 0.55f;

    [Tooltip("Bắt đầu shrink từ t (0..1). Ví dụ 0.55 nghĩa là nửa đầu không shrink nhiều.")]
    [Range(0f, 1f)]
    [SerializeField] private float shrinkStart = 0.55f;

    [Tooltip("Độ gắt shrink (càng cao càng hút mạnh).")]
    [SerializeField] private float shrinkPower = 2.2f;

    [Header("Finish")]
    [SerializeField] private bool destroyOnArrive = false;

    private Coroutine _co;
    private Vector2 _currentPos;

    private void Awake()
    {
        if (rect == null) rect = (RectTransform)transform;
    }

    public void ResetVisual(Vector2 start)
    {
        gameObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        rect.anchoredPosition = start;
        rect.localScale = Vector3.one;
        _currentPos = start;
    }

    public void BurstTo(Vector2 burstPos, float burstDuration)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoBurst(_currentPos, burstPos, burstDuration));
    }

    public void FlyTo(Vector2 end, float flyDuration, Action onArrive)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFlyRandomCubic(_currentPos, end, flyDuration, onArrive));
    }

    private IEnumerator CoBurst(Vector2 start, Vector2 burstPos, float dur)
    {
        dur = Mathf.Max(0.02f, dur);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            float e = EaseOutCubic(u);

            Vector2 p = Vector2.LerpUnclamped(start, burstPos, e);
            rect.anchoredPosition = p;
            _currentPos = p;

            float s = 1f + Mathf.Sin(u * Mathf.PI) * burstPunchScale;
            rect.localScale = Vector3.one * s;

            yield return null;
        }

        rect.anchoredPosition = burstPos;
        rect.localScale = Vector3.one;
        _currentPos = burstPos;
        _co = null;
    }

    private IEnumerator CoFlyRandomCubic(Vector2 from, Vector2 to, float dur, Action onArrive)
    {
        dur = Mathf.Max(0.05f, dur);

        Vector2 p0 = from;
        Vector2 p2 = to;

        Vector2 d = (p2 - p0);
        float dist = Mathf.Max(1f, d.magnitude);
        Vector2 dir = d / dist;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        // ✅ 1 control point tại 1/3 đoạn đường kể từ start
        Vector2 baseAtOneThird = Vector2.Lerp(p0, p2, 1f / 3f);

        // độ cong: chọn 1 phía, random biên độ
        float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;

        // bạn có thể tune clamp theo ý: cong vừa đủ, không quá “văng”
        float bend = dist * startBendFactor + UnityEngine.Random.Range(-bendRandomPx, bendRandomPx);
        bend = Mathf.Clamp(bend, 80f, 260f);

        Vector2 p1 = baseAtOneThird + perp * (sign * bend);

        // wobble
        float wobbleFreq = UnityEngine.Random.Range(wobbleFreqMin, wobbleFreqMax);
        float wobblePhase = UnityEngine.Random.Range(0f, 10f);

        Vector2 wobbleAxis = UnityEngine.Random.insideUnitCircle.normalized;
        if (wobbleAxis.sqrMagnitude < 0.01f) wobbleAxis = perp;

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);

            // ✅ cuối đường nhọn hơn: tăng tốc về cuối
            float u2 = EaseInQuart(u);
            float e = EaseOutCubic(u2);

            Vector2 p = Bezier2(p0, p1, p2, e);

            // ✅ wobble tắt mạnh về cuối để “nhọn” vào target
            float w = Mathf.Sin((u * wobbleFreq + wobblePhase) * Mathf.PI * 2f);
            float tail = 1f - Mathf.SmoothStep(0.70f, 1f, u);
            p += wobbleAxis * w  * tail;

            rect.anchoredPosition = p;
            _currentPos = p;

            float shrink = ComputeShrink(u);
            rect.localScale = Vector3.one  * shrink;

            yield return null;
        }

        rect.anchoredPosition = to;
        rect.localScale = Vector3.one * Mathf.Max(0.01f, endScale);
        _currentPos = to;

        onArrive?.Invoke();

        if (canvasGroup != null)
        {
            float ft = 0f;
            const float fadeDur = 1f;
            while (ft < fadeDur)
            {
                ft += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - (ft / fadeDur);
                yield return null;
            }
        }

        if (destroyOnArrive) Destroy(gameObject);
        else gameObject.SetActive(false);

        _co = null;
    }

    // ===== Helpers =====

    private static Vector2 Bezier2(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        // Quadratic Bezier: (1-t)^2 p0 + 2(1-t)t p1 + t^2 p2
        float a = 1f - t;
        return a * a * p0 + 2f * a * t * p1 + t * t * p2;
    }

    private static float EaseInQuart(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * x * x;
    }

    private float ComputeShrink(float u)
    {
        // u < shrinkStart => shrink gần như 1
        if (u <= shrinkStart) return 1f;

        float t = Mathf.InverseLerp(shrinkStart, 1f, u); // 0..1
        // làm gắt hơn
        t = Mathf.Pow(t, Mathf.Max(0.01f, shrinkPower));
        // 1 -> endScale
        return Mathf.Lerp(1f, Mathf.Clamp(endScale, 0.05f, 1f), t);
    }

    private static float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        float a = 1f - x;
        return 1f - a * a * a;
    }
}