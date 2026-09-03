using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Thinh.Base
{
    public sealed class FXController : MonoSingleton<FXController>
    {
        [Header("UI Canvas")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private bool autoFindCanvas = true;

        [Header("UI FX Timing")]
        [SerializeField] private bool uiFxUseUnscaledTime = true;

        [Header("Optional UI Particle Component")]
        [Tooltip("Tên class UI Particle component (ví dụ: UIParticle). Để trống nếu không dùng.")]
        [SerializeField] private string uiParticleTypeName = "UIParticle";

        [Header("Pooling")]
        [SerializeField] private bool parentPooledToController = true;

        // pool theo prefab instanceID
        private readonly Dictionary<int, Queue<GameObject>> _pool = new();
        private readonly Dictionary<int, GameObject> _prefabById = new();

        public override void Init()
        {
            EnsureCanvas();
        }

        private void EnsureCanvas()
        {
            if (uiCanvas != null) return;

            if (autoFindCanvas)
            {
                // Lấy canvas đầu tiên trong scene (bạn có thể custom logic: theo tag/name)
                uiCanvas = FindObjectOfType<Canvas>();
            }

            if (uiCanvas == null)
            {
                Debug.LogError("[FXController] uiCanvas is null. Please assign a Canvas in Inspector (or enable autoFindCanvas).");
            }
        }

        // =========================================================
        // PUBLIC API
        // =========================================================

        /// <summary>
        /// Spawn UI FX vào Canvas tại anchoredPosition + scale.
        /// anchoredPosition: tọa độ local trong RectTransform của canvas.
        /// </summary>
        public GameObject ShowUIFX(GameObject fxPrefab, Vector2 anchoredPosition, float scale = 1f, int siblingIndex = -1)
        {
            if (fxPrefab == null)
            {
                Debug.LogWarning("[FXController] ShowUIFX: fxPrefab is null.");
                return null;
            }

            EnsureCanvas();
            if (uiCanvas == null) return null;

            var go = SpawnFromPool(fxPrefab);

            // parent vào canvas
            var t = go.transform;
            t.SetParent(uiCanvas.transform, false);

            // đảm bảo RectTransform + set pos/scale
            var rt = EnsureRectTransform(go);
            rt.anchoredPosition = anchoredPosition;
            rt.localScale = Vector3.one * Mathf.Max(0.0001f, scale);

            if (siblingIndex >= 0) rt.SetSiblingIndex(siblingIndex);

            // optional: add UI Particle component
            TryEnsureUIParticle(go);

            // play + auto return
            PlayAllParticles(go);
            AttachOrGetAutoReturn(go).Begin(this, fxPrefab, uiFxUseUnscaledTime);

            return go;
        }

        /// <summary>
        /// Quick overload: center + scale 1.
        /// </summary>
        public GameObject ShowUIFX(GameObject fxPrefab)
            => ShowUIFX(fxPrefab, Vector2.zero, 1f, -1);

        /// <summary>
        /// Spawn World FX tại world position + rotation + scale.
        /// </summary>
        public GameObject ShowWorldFX(GameObject fxPrefab, Vector3 worldPosition, Quaternion rotation, float scale = 1f)
        {
            if (fxPrefab == null)
            {
                Debug.LogWarning("[FXController] ShowWorldFX: fxPrefab is null.");
                return null;
            }

            var go = SpawnFromPool(fxPrefab);

            var t = go.transform;
            t.SetParent(null);
            t.position = worldPosition;
            t.rotation = rotation;
            t.localScale = Vector3.one * Mathf.Max(0.0001f, scale);

            PlayAllParticles(go);
            AttachOrGetAutoReturn(go).Begin(this, fxPrefab, useUnscaledTime: false);

            return go;
        }

        // =========================================================
        // POOL CORE
        // =========================================================

        private GameObject SpawnFromPool(GameObject prefab)
        {
            int id = prefab.GetInstanceID();
            _prefabById[id] = prefab;

            if (_pool.TryGetValue(id, out var q) && q.Count > 0)
            {
                var go = q.Dequeue();
                if (go == null) return Instantiate(prefab);

                go.SetActive(true);
                return go;
            }

            return Instantiate(prefab);
        }

        internal void DespawnToPool(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;

            int id = prefab.GetInstanceID();
            if (!_pool.TryGetValue(id, out var q))
            {
                q = new Queue<GameObject>();
                _pool[id] = q;
            }

            StopAllParticles(instance);

            instance.SetActive(false);

            if (parentPooledToController)
                instance.transform.SetParent(transform, false);

            q.Enqueue(instance);
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            if (!go.TryGetComponent<RectTransform>(out var rt))
                rt = go.AddComponent<RectTransform>();

            // default UI pivot/anchor center
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localRotation = Quaternion.identity;

            return rt;
        }

        private void TryEnsureUIParticle(GameObject go)
        {
            if (string.IsNullOrWhiteSpace(uiParticleTypeName)) return;

            var type = FindTypeByName(uiParticleTypeName);
            if (type == null) return;

            if (go.GetComponent(type) != null) return;
            go.AddComponent(type);
        }

        private static Type FindTypeByName(string typeName)
        {
            // try full name
            var t = Type.GetType(typeName);
            if (t != null) return t;

            // scan assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try { types = assemblies[i].GetTypes(); }
                catch { continue; }

                for (int j = 0; j < types.Length; j++)
                {
                    if (types[j].Name == typeName) return types[j];
                }
            }

            return null;
        }

        private static void PlayAllParticles(GameObject go)
        {
            var ps = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++)
            {
                ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps[i].Play(true);
            }
        }

        private static void StopAllParticles(GameObject go)
        {
            var ps = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++)
            {
                ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static FXAutoReturn AttachOrGetAutoReturn(GameObject go)
        {
            if (!go.TryGetComponent<FXAutoReturn>(out var r))
                r = go.AddComponent<FXAutoReturn>();
            return r;
        }

        // =========================================================
        // AUTO RETURN COMPONENT
        // =========================================================
        private sealed class FXAutoReturn : MonoBehaviour
        {
            private FXController _owner;
            private GameObject _prefab;
            private Coroutine _co;

            public void Begin(FXController owner, GameObject prefab, bool useUnscaledTime)
            {
                _owner = owner;
                _prefab = prefab;

                if (_co != null) StopCoroutine(_co);
                _co = StartCoroutine(CoWaitAndReturn(useUnscaledTime));
            }

            private IEnumerator CoWaitAndReturn(bool useUnscaledTime)
            {
                float wait = CalculateMaxParticleTime(gameObject);
                wait = Mathf.Max(0.05f, wait);

                if (useUnscaledTime)
                {
                    float t = 0f;
                    while (t < wait)
                    {
                        t += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(wait);
                }

                if (_owner != null && _prefab != null)
                    _owner.DespawnToPool(gameObject, _prefab);
                else
                    gameObject.SetActive(false);

                _co = null;
            }

            private static float CalculateMaxParticleTime(GameObject root)
            {
                float max = 0f;
                var ps = root.GetComponentsInChildren<ParticleSystem>(true);

                for (int i = 0; i < ps.Length; i++)
                {
                    var main = ps[i].main;

                    float duration = main.duration;

                    float lifeMax = 0f;
                    var life = main.startLifetime;
                    if (life.mode == ParticleSystemCurveMode.Constant) lifeMax = life.constant;
                    else if (life.mode == ParticleSystemCurveMode.TwoConstants) lifeMax = Mathf.Max(life.constantMin, life.constantMax);
                    else lifeMax = 1f; // curve: fallback

                    float total = duration + lifeMax;

                    // Nếu loop, vẫn return theo duration+lifetime (tuỳ bạn). Nếu bạn muốn loop không return => return rất lớn.
                    if (total > max) max = total;
                }

                return max > 0f ? max : 0.1f;
            }
        }
    }
}