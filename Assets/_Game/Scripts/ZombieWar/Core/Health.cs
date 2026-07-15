using System;
using UnityEngine;

namespace ZombieWar.Core
{
    [Serializable]
    public sealed class Health
    {
        [SerializeField] private float maxHealth = 100f;

        public event Action<float, float> Changed;
        public event Action Depleted;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsAlive => Current > 0f;
        public float Normalized => maxHealth > 0f ? Mathf.Clamp01(Current / maxHealth) : 0f;

        public void Initialize()
        {
            Current = Mathf.Max(1f, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        public void SetMaxHealth(float value, bool refill)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refill)
            {
                Current = maxHealth;
            }
            else
            {
                Current = Mathf.Clamp(Current, 0f, maxHealth);
            }

            Changed?.Invoke(Current, maxHealth);
        }

        public bool ApplyDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return false;
            }

            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke(Current, maxHealth);

            if (Current <= 0f)
            {
                Depleted?.Invoke();
            }

            return true;
        }
    }
}
