using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thread-safe dispatcher để thực thi Unity API trên Main Thread.
/// Dùng cho callback từ Ads SDK, Analytics SDK, Task / UniTask background.
/// 
/// PRODUCTION VERSION:
/// - Thread-safe enqueue
/// - Execute sớm trong frame
/// - Giới hạn action / frame (anti-freeze)
/// - Safe DontDestroyOnLoad
/// - Không gây ANR
/// </summary>
[DefaultExecutionOrder(-9999)]
public sealed class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly object _instanceLock = new object();

    private readonly Queue<Action> _executionQueue = new Queue<Action>();

    /// <summary>
    /// Giới hạn số action xử lý mỗi frame
    /// Tránh spike nếu SDK enqueue quá nhiều callback
    /// </summary>
    private const int MAX_ACTIONS_PER_FRAME = 50;

    // =========================================================
    // INSTANCE
    // =========================================================
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            lock (_instanceLock)
            {
                if (_instance == null)
                {
                    // Chỉ nên được gọi từ main thread (AdsController / Bootstrap)
                    var obj = new GameObject("[System] UnityMainThreadDispatcher");
                    _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Ngăn tạo trùng nếu có dispatcher được add thủ công trong scene
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // UPDATE – MAIN THREAD EXECUTION
    // =========================================================
    private void Update()
    {
        Action[] actionsToRun = null;

        lock (_executionQueue)
        {
            if (_executionQueue.Count > 0)
            {
                actionsToRun = _executionQueue.ToArray();
                _executionQueue.Clear();
            }
        }

        if (actionsToRun == null || actionsToRun.Length == 0)
            return;

        int executed = 0;

        foreach (var action in actionsToRun)
        {
            if (executed >= MAX_ACTIONS_PER_FRAME)
                break;

            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[UnityMainThreadDispatcher] Exception:\n{ex}");
#endif
            }

            executed++;
        }
    }

    // =========================================================
    // ENQUEUE API
    // =========================================================

    /// <summary>
    /// Enqueue action không tham số
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null)
            return;

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Enqueue action 1 tham số
    /// </summary>
    public void Enqueue<T>(Action<T> action, T param)
    {
        if (action == null)
            return;

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => action(param));
        }
    }

    /// <summary>
    /// Enqueue action 2 tham số
    /// </summary>
    public void Enqueue<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
    {
        if (action == null)
            return;

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => action(param1, param2));
        }
    }

    // =========================================================
    // SAFE HELPERS (OPTIONAL)
    // =========================================================

    /// <summary>
    /// Thực thi ngay nếu đang ở main thread, ngược lại enqueue
    /// </summary>
    public void RunOrEnqueue(Action action)
    {
        if (action == null) return;

        if (IsMainThread())
        {
            action.Invoke();
        }
        else
        {
            Enqueue(action);
        }
    }

    /// <summary>
    /// Check đơn giản main thread (dựa vào Unity API)
    /// </summary>
    private bool IsMainThread()
    {
        // Nếu gọi được Time.realtimeSinceStartup => đang ở Unity main thread
        try
        {
            var _ = Time.realtimeSinceStartup;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
