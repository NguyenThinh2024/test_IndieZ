using System;
using UnityEngine;

namespace Nexzap.Base.Gameplay
{
    /// <summary>
    /// Combo = chuỗi hit trong khoảng thời gian.
    /// Mỗi hit sẽ reset countdown và tăng combo.
    /// Sao nhận được = baseStarPerHit * combo (hoặc theo curve).
    /// Đồng thời bắn event để UI spawn star-fly.
    /// </summary>
    public sealed class ComboService : GameplayServiceBehaviour
    {
        [Header("Combo Rules")]
        [SerializeField] private float comboWindowSeconds = 3f;
        [SerializeField] private int maxCombo = 99;

        [Header("Star Reward")]
        [SerializeField] private int baseStarPerHit = 1;
        [Tooltip("Nếu bật: sao = baseStarPerHit * comboCurrent. Nếu tắt: sao = baseStarPerHit.")]
        [SerializeField] private bool multiplyByCombo = true;

        [Header("Optional UI Auto Bind")]
        [SerializeField] private bool autoBindUI = true;
        [SerializeField] private UICombo uiCombo; // optional

        public int Combo { get; private set; }          // combo hiện tại
        public float TimeLeft { get; private set; }     // countdown còn lại
        public int StarTotal { get; private set; }      // tổng sao tích lũy (session)

        public bool IsActive => Combo > 0 && TimeLeft > 0f;

        /// UI update: (combo, timeLeft, window, starTotal)
        public event Action<int, float, float, int> OnChanged;

        /// Khi có sao mới: (amountGained, newTotal, comboAtThatMoment)
        public event Action<int, int, int> OnStarGained;

        /// Spawn star fly effect: (count, worldFrom, worldTo)
        public event Action<int, Vector3> OnStarFly;

        public override void OnStart()
        {
            Combo = 0;
            TimeLeft = 0f;
            StarTotal = 0;

            if (autoBindUI) AutoBindUI();
            PushChanged();
        }

        public override void Tick(float dt)
        {
            if (Combo <= 0) return;

            TimeLeft -= dt;
            if (TimeLeft <= 0f)
            {
                ResetCombo();
                return;
            }

            PushChanged();
        }

        /// <summary>
        /// Gameplay gọi khi người chơi tạo 1 hit thuộc combo chain.
        /// from/to: vị trí bay sao (world/canvas tuỳ bạn thống nhất; UI có thể convert).
        /// </summary>
        public void NotifyComboHit(Vector3 flyFrom, int extraStarBonus = 0)
        {
            // tăng combo + reset timer
            Combo = Mathf.Clamp(Combo + 1, 1, maxCombo);
            TimeLeft = comboWindowSeconds;

            // tính sao
            int gained = baseStarPerHit + extraStarBonus;
            if (multiplyByCombo) gained *= Combo;

            StarTotal += gained;

            // bắn event
            OnStarGained?.Invoke(gained, StarTotal, Combo);

            // star fly count (bao nhiêu ngôi sao bay) – thường không nên bay = gained (vì có thể quá nhiều)
            int flyCount = 1;
            OnStarFly?.Invoke(flyCount, flyFrom);

            PushChanged();
        }

        public void ResetCombo()
        {
            Combo = 0;
            TimeLeft = 0f;
            PushChanged();
        }

        public void AddStars(int amount)
        {
            if (amount <= 0) return;
            StarTotal += amount;
            OnStarGained?.Invoke(amount, StarTotal, Combo);
            PushChanged();
        }

        private void PushChanged()
        {
            OnChanged?.Invoke(Combo, Mathf.Max(0f, TimeLeft), comboWindowSeconds, StarTotal);
        }

        private void AutoBindUI()
        {
#if UNITY_2022_2_OR_NEWER
            if (uiCombo == null)
                uiCombo = UnityEngine.Object.FindFirstObjectByType<UICombo>(FindObjectsInactive.Include);
#else
            if (uiCombo == null)
                uiCombo = UnityEngine.Object.FindObjectOfType<UICombo>(true);
#endif
            if (uiCombo != null) uiCombo.Bind(this);
        }
    }
}