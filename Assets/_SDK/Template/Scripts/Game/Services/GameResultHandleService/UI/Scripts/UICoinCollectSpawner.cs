using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UICoinCollectSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform headerCoinTarget; // spawn root + end point
    [SerializeField] private UICoinFlyItem coinFlyPrefab;

    [Header("Count")]
    [SerializeField] private int maxCoinsSpawn = 12;
    [SerializeField] private int minCoinsSpawn = 4;

    [Header("Burst")]
    [SerializeField] private float spawnRadius = 200f;
    [SerializeField] private float burstDuration = 0.2f;

    [Tooltip("Đợi sau khi bung xong rồi mới bay (cả đàn cùng bay).")]
    [SerializeField] private float burstHold = 0.5f;

    [Header("Fly")]
    [Tooltip("Min phải < Max")]
    [SerializeField] private float flyDurationMin = 0.35f;
    [SerializeField] private float flyDurationMax = 0.75f;

    [Tooltip("Nếu muốn coin bay lệch nhịp random nhẹ. 0 = bay cùng lúc.")]
    [SerializeField] private float flyStaggerMin = 0.00f;
    [SerializeField] private float flyStaggerMax = 0.04f;

    private readonly Queue<UICoinFlyItem> _pool = new();
    private RectTransform SpawnRoot => headerCoinTarget;

    private void OnEnable()
    {
        foreach (var pool in _pool)
        {
            pool.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Start từ UI anchoredPosition (TRONG local-space của headerCoinTarget).
    /// amount được chia ra cho từng coin và trả về callback onEachArrive(add).
    /// </summary>
    public void CollectCoinsFromUI(Vector2 startAnchoredPosInTarget, int amount, Action<int> onEachArrive, Action onAllArrived = null)
    {
        if (SpawnRoot == null || coinFlyPrefab == null) return;
        if (amount <= 0) return;

        int coinCount = CalcCoinCount(amount);
        SplitAmount(amount, coinCount, out int baseAdd, out int remainder);

        StopAllCoroutines();
        StartCoroutine(CoBurstHoldFly(startAnchoredPosInTarget, coinCount, baseAdd, remainder, onEachArrive, onAllArrived));
    }

    public void CollectCoinsFromWorld(Vector3 worldStart, int amount, Camera cam, Action<int> onEachArrive, Action onAllArrived = null)
    {
        if (SpawnRoot == null || coinFlyPrefab == null) return;
        if (amount <= 0) return;

        Vector2 start = WorldToRectAnchored(SpawnRoot, worldStart, cam);

        int coinCount = CalcCoinCount(amount);
        SplitAmount(amount, coinCount, out int baseAdd, out int remainder);

        StopAllCoroutines();
        StartCoroutine(CoBurstHoldFly(start, coinCount, baseAdd, remainder, onEachArrive, onAllArrived));
    }

    private IEnumerator CoBurstHoldFly(
        Vector2 start,
        int coinCount,
        int baseAdd,
        int remainder,
        Action<int> onEachArrive,
        Action onAllArrived)
    {
        Vector2 end = Vector2.zero;

        // 1) Spawn + Burst đồng loạt
        var items = new List<(UICoinFlyItem item, int add)>(coinCount);

        for (int i = 0; i < coinCount; i++)
        {
            var item = Get();
            item.ResetVisual(start);

            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector2 burstPos = start + offset;

            int add = baseAdd + (i < remainder ? 1 : 0);

            item.BurstTo(burstPos, burstDuration);
            items.Add((item, add));
        }

        if (burstDuration > 0f)
            yield return new WaitForSecondsRealtime(burstDuration);

        if (burstHold > 0f)
            yield return new WaitForSecondsRealtime(burstHold);

        // 2) Fly đồng loạt (option stagger nhẹ)
        int arrived = 0;

        float min = Mathf.Min(flyDurationMin, flyDurationMax);
        float max = Mathf.Max(flyDurationMin, flyDurationMax);

        for (int i = 0; i < items.Count; i++)
        {
            var pack = items[i];
            float flyDur = UnityEngine.Random.Range(min, max);

            pack.item.FlyTo(end, flyDur, onArrive: () =>
            {
                onEachArrive?.Invoke(pack.add);
                arrived++;
                if (arrived >= items.Count)
                    onAllArrived?.Invoke();
            });

            float stagger = UnityEngine.Random.Range(flyStaggerMin, flyStaggerMax);
            if (stagger > 0f)
                yield return new WaitForSecondsRealtime(stagger);
        }
    }

    private int CalcCoinCount(int amount)
    {
        int c = Mathf.CeilToInt(amount / 5f);
        return Mathf.Clamp(c, minCoinsSpawn, maxCoinsSpawn);
    }

    private static void SplitAmount(int amount, int coinCount, out int baseAdd, out int remainder)
    {
        baseAdd = amount / coinCount;
        remainder = amount % coinCount;
    }

    private UICoinFlyItem Get()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        var inst = Instantiate(coinFlyPrefab, SpawnRoot);
        inst.gameObject.SetActive(false);

        var rt = inst.transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        return inst;
    }

    private static Vector2 WorldToRectAnchored(RectTransform rect, Vector3 worldPos, Camera cam)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, cam, out var local);
        return local;
    }
}