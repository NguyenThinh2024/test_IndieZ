using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Thinh.Base.Data;
using Thinh.Base.UI;

namespace Thinh.Base.UI.Template
{
    public class UICoinHeader : MonoBehaviour
    {
        [SerializeField] private UIBaseButton plusBtn;
        [SerializeField] private TextMeshProUGUI coinText;

        [Header("Collect Coin FX")]
        [SerializeField] private UICoinCollectSpawner coinCollectSpawner;

        [Tooltip("Target icon (punch). Nếu null -> coinText.rectTransform")]
        [SerializeField] private RectTransform coinTarget;

        [Header("Punch")]
        [SerializeField] private float punchScale = 0.12f;
        [SerializeField] private float punchDuration = 0.12f;

        [Tooltip("Chặn punch quá dày khi nhiều coin arrive liên tục. (0 = không chặn)")]
        [SerializeField] private float punchCooldown = 0.06f;

        private bool _animating;
        private int _displayCoins;
        private int _targetCoins;

        private Coroutine _punchCo;

        // ✅ Fix “phình to”: luôn punch dựa trên scale gốc
        private Vector3 _baseScale;
        private bool _baseScaleCaptured;
        private float _nextPunchTime;

        private void Start()
        {
            FetchData(null);
        }

        private void OnEnable()
        {
            if (UserProfileController.Instance != null)
                UserProfileController.Instance.OnUserChanged.AddListener(FetchData);

            if (plusBtn != null)
                plusBtn.onClick.AddListener(OnClickPlus);

            CaptureBaseScale();
        }

        private void OnDisable()
        {
            UserProfileController.Instance?.OnUserChanged?.RemoveListener(FetchData);
            plusBtn?.onClick?.RemoveListener(OnClickPlus);

            if (_punchCo != null)
            {
                StopCoroutine(_punchCo);
                _punchCo = null;
            }

            // reset scale về base nếu có
            var rt = GetPunchTarget();
            if (rt != null && _baseScaleCaptured)
                rt.localScale = _baseScale;
        }

        private void FetchData(object _)
        {
            // ✅ Khi đang animate, bỏ qua sync theo data để tránh text nhảy thẳng
            if (_animating) return;
            UpdateUIFromData();
        }

        private void UpdateUIFromData()
        {
            var user = UserProfileController.Instance;
            if (user == null) return;

            int coins = (int)user.userData.coin;
            SetCoinText(coins);
        }

        /// <summary>
        /// Cộng coin "thẳng" (không FX).
        /// </summary>
        public void AddCoinInstant(int amount)
        {
            if (amount <= 0) return;

            var user = UserProfileController.Instance;
            if (user == null) return;

            user.AddCoin(amount);
            // UI sẽ update qua OnUserChanged (nếu không animating)
            Punch();
        }

        /// <summary>
        /// ✅ Cộng data trước, rồi coin bay vào từng đồng để tăng UI dần.
        /// startAnchoredPosInHeaderTarget: phải là local-space của headerCoinTarget (SpawnRoot).
        /// </summary>
        public void CollectCoinsFromUI(Vector2 startAnchoredPosInHeaderTarget, int amount)
        {
            if (amount <= 0) return;

            var user = UserProfileController.Instance;
            if (user == null) return;

            int startCoins = (int)user.userData.coin;

            _displayCoins = startCoins;
            _targetCoins = startCoins + amount;

            // ✅ 1) cộng data ngay lập tức
            user.AddCoin(amount);

            // ✅ 2) khóa UI sync theo data, show từ startCoins
            _animating = true;
            SetCoinText(_displayCoins);

            if (coinCollectSpawner != null)
            {
                coinCollectSpawner.CollectCoinsFromUI(
                    startAnchoredPosInHeaderTarget,
                    amount,
                    onEachArrive: OnEachCoinArrive,
                    onAllArrived: OnAllArrived
                );
            }
            else
            {
                // fallback: không có spawner thì set thẳng UI = target
                _displayCoins = _targetCoins;
                SetCoinText(_displayCoins);
                _animating = false;
                Punch();
            }
        }

        /// <summary>
        /// Bắt đầu từ world pos (cần camera UI/world phù hợp).
        /// </summary>
        public void CollectCoinsFromWorld(Vector3 worldStart, int amount, Camera cam)
        {
            if (amount <= 0) return;

            var user = UserProfileController.Instance;
            if (user == null) return;

            int startCoins = (int)user.userData.coin;

            _displayCoins = startCoins;
            _targetCoins = startCoins + amount;

            // ✅ cộng data trước
            user.AddCoin(amount);

            _animating = true;
            SetCoinText(_displayCoins);

            if (coinCollectSpawner != null)
            {
                coinCollectSpawner.CollectCoinsFromWorld(
                    worldStart,
                    amount,
                    cam,
                    onEachArrive: OnEachCoinArrive,
                    onAllArrived: OnAllArrived
                );
            }
            else
            {
                _displayCoins = _targetCoins;
                SetCoinText(_displayCoins);
                _animating = false;
                Punch();
            }
        }

        private void OnEachCoinArrive(int add)
        {
            if (add <= 0) return;

            _displayCoins += add;
            if (_displayCoins > _targetCoins) _displayCoins = _targetCoins;

            SetCoinText(_displayCoins);
            Punch();

            AudioController.Instance.PlaySound(SoundName.UI_CollectCoin);
        }

        private void OnAllArrived()
        {
            // ✅ Ép UI đúng tuyệt đối sau cùng (chống lệch do rounding/remainder)
            _displayCoins = _targetCoins;
            SetCoinText(_displayCoins);

            _animating = false;

            // Sau khi unlock, nếu data còn thay đổi ở chỗ khác, FetchData sẽ sync bình thường
        }

        private void SetCoinText(int value)
        {
            if (coinText != null)
                coinText.text = $"{value}";
        }

        // =========================
        // Punch (FIX phình to)
        // =========================

        private RectTransform GetPunchTarget()
        {
            return coinTarget != null ? coinTarget : (coinText != null ? coinText.rectTransform : null);
        }

        private void CaptureBaseScale()
        {
            var rt = GetPunchTarget();
            if (rt == null) return;

            _baseScale = rt.localScale;
            _baseScaleCaptured = true;
        }

        private void Punch()
        {
            var rt = GetPunchTarget();
            if (rt == null) return;

            // optional throttle
            if (punchCooldown > 0f && Time.unscaledTime < _nextPunchTime)
                return;

            _nextPunchTime = Time.unscaledTime + Mathf.Max(0f, punchCooldown);

            if (!_baseScaleCaptured)
            {
                _baseScale = rt.localScale;
                _baseScaleCaptured = true;
            }

            // ✅ stop punch cũ, reset về baseScale để không phình
            if (_punchCo != null) StopCoroutine(_punchCo);
            rt.localScale = _baseScale;

            _punchCo = StartCoroutine(CoPunch(rt, _baseScale));
        }

        private IEnumerator CoPunch(RectTransform rt, Vector3 baseScale)
        {
            Vector3 upScale = baseScale * (1f + punchScale);
            float dur = Mathf.Max(0.05f, punchDuration);

            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = t / dur;
                rt.localScale = Vector3.LerpUnclamped(baseScale, upScale, EaseOutCubic(u));
                yield return null;
            }

            t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = t / dur;
                rt.localScale = Vector3.LerpUnclamped(upScale, baseScale, EaseOutCubic(u));
                yield return null;
            }

            rt.localScale = baseScale;
        }

        private static float EaseOutCubic(float x)
        {
            x = Mathf.Clamp01(x);
            float a = 1f - x;
            return 1f - a * a * a;
        }

        private void OnClickPlus()
        {
            AudioController.Instance.PlaySound(SoundName.UI_ButtonClick);
            UIPopupController.Instance.GetActivePopup<UIPopupGetCoins>();
        }
    }
}