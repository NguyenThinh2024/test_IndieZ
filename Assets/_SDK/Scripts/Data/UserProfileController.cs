using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Nexzap.Template;

namespace Nexzap.Base.Data
{
    public sealed class UserProfileController : MonoSingleton<UserProfileController>
    {
        private const string CurrentLevelKey = "currentLevel";
        private const string CoinKey = "coin";
        private const string LifeKey = "life";
        private const string TimeToAddLifeKey = "timeToAddLife";
        private const string WinStreakKey = "winStreak";

        public readonly UnityEvent<object> OnUserChanged = new();

        public UserData userData { get; private set; } = new UserData();

        public int LEVEL
        {
            get => Mathf.Max(1, GetParam<int>(CurrentLevelKey));
            set
            {
                SetParam(CurrentLevelKey, Mathf.Max(1, value));
                RaiseUserChanged();
            }
        }

        public bool HasLiveInfinity => LiveInfinityRemainingSeconds > 0;

        public int LiveInfinityRemainingSeconds
        {
            get
            {
                long remaining = userData.life.liveInfinityEndUnixTime - GetUnixTimeSeconds();
                return remaining > 0 ? (int)remaining : 0;
            }
        }

        public int NextRefillRemainSeconds
        {
            get
            {
                long remaining = userData.life.nextRefillUnixTime - GetUnixTimeSeconds();
                return remaining > 0 ? (int)remaining : 0;
            }
        }

        public override void Init()
        {
            base.Init();
            LoadFromPlayerPrefs();
            UpdateLives();
        }

        public void LoadUserProfile(Action<UserData> onLoaded)
        {
            LoadFromPlayerPrefs();
            UpdateLives();
            onLoaded?.Invoke(userData);
        }

        public T GetParam<T>(string key)
        {
            Type type = typeof(T);
            if (type == typeof(bool))
            {
                object value = PlayerPrefs.GetInt(key, 0) != 0;
                return (T)value;
            }

            if (type == typeof(int))
            {
                object value = PlayerPrefs.GetInt(key, 0);
                return (T)value;
            }

            if (type == typeof(float))
            {
                object value = PlayerPrefs.GetFloat(key, 0f);
                return (T)value;
            }

            if (type == typeof(string))
            {
                object value = PlayerPrefs.GetString(key, string.Empty);
                return (T)value;
            }

            return default;
        }

        public void SetParam<T>(string key, T value)
        {
            switch (value)
            {
                case bool boolValue:
                    PlayerPrefs.SetInt(key, boolValue ? 1 : 0);
                    break;
                case int intValue:
                    PlayerPrefs.SetInt(key, intValue);
                    break;
                case float floatValue:
                    PlayerPrefs.SetFloat(key, floatValue);
                    break;
                case string stringValue:
                    PlayerPrefs.SetString(key, stringValue);
                    break;
                default:
                    return;
            }

            SyncKnownValue(key);
            PlayerPrefs.Save();
            RaiseUserChanged();
        }

        public void AddCoin(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetCoin(userData.coin + amount);
        }

        public bool UseCoin(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (userData.coin < amount)
            {
                return false;
            }

            SetCoin(userData.coin - amount);
            return true;
        }

        public bool UseCoin(long amount)
        {
            if (amount <= 0L)
            {
                return true;
            }

            if (amount > int.MaxValue || userData.coin < amount)
            {
                return false;
            }

            SetCoin(userData.coin - (int)amount);
            return true;
        }

        public void AddLife(int amount)
        {
            if (amount <= 0 || HasLiveInfinity)
            {
                return;
            }

            int maxLives = GetMaxLives();
            userData.life.liveAmount = Mathf.Clamp(userData.life.liveAmount + amount, 0, maxLives);
            PersistLife();
            RaiseUserChanged();
        }

        public bool UseLife()
        {
            UpdateLives();
            if (HasLiveInfinity)
            {
                return true;
            }

            if (userData.life.liveAmount <= 0)
            {
                return false;
            }

            userData.life.liveAmount--;
            ScheduleNextLifeIfNeeded();
            PersistLife();
            RaiseUserChanged();
            return true;
        }

        public void RefillLives()
        {
            userData.life.liveAmount = GetMaxLives();
            userData.life.nextRefillUnixTime = 0;
            PersistLife();
            RaiseUserChanged();
        }

        public void UpdateLives()
        {
            if (HasLiveInfinity)
            {
                return;
            }

            int maxLives = GetMaxLives();
            if (userData.life.liveAmount >= maxLives)
            {
                userData.life.liveAmount = maxLives;
                userData.life.nextRefillUnixTime = 0;
                PersistLife();
                return;
            }

            long now = GetUnixTimeSeconds();
            int refillInterval = GetRefillInterval();
            if (userData.life.nextRefillUnixTime <= 0)
            {
                userData.life.nextRefillUnixTime = now + refillInterval;
                PersistLife();
                return;
            }

            if (now < userData.life.nextRefillUnixTime)
            {
                return;
            }

            long elapsed = now - userData.life.nextRefillUnixTime;
            int refillCount = 1 + (int)(elapsed / refillInterval);
            userData.life.liveAmount = Mathf.Min(maxLives, userData.life.liveAmount + refillCount);
            userData.life.nextRefillUnixTime = userData.life.liveAmount >= maxLives
                ? 0
                : userData.life.nextRefillUnixTime + refillCount * refillInterval;

            PersistLife();
            RaiseUserChanged();
        }

        public void ResetWinStreak()
        {
            SetParam(WinStreakKey, 0);
        }

        public int GetBoosterAmount(BoosterType boosterType)
        {
            return GetParam<int>(GetBoosterKey(boosterType));
        }

        public void AddBooster(BoosterType boosterType, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetParam(GetBoosterKey(boosterType), GetBoosterAmount(boosterType) + amount);
        }

        public bool UseBooster(BoosterType boosterType)
        {
            string key = GetBoosterKey(boosterType);
            int amount = GetParam<int>(key);
            if (amount <= 0)
            {
                return false;
            }

            SetParam(key, amount - 1);
            return true;
        }

        private void LoadFromPlayerPrefs()
        {
            userData.coin = PlayerPrefs.GetInt(CoinKey, 0);
            userData.life.liveAmount = PlayerPrefs.GetInt(LifeKey, GetMaxLives());
            userData.life.nextRefillUnixTime = long.Parse(PlayerPrefs.GetString("life_next_refill", "0"));
            userData.life.liveInfinityEndUnixTime = long.Parse(PlayerPrefs.GetString("life_infinity_end", "0"));

            if (!PlayerPrefs.HasKey(CurrentLevelKey))
            {
                PlayerPrefs.SetInt(CurrentLevelKey, 1);
            }
        }

        private void SyncKnownValue(string key)
        {
            if (key == CoinKey)
            {
                userData.coin = PlayerPrefs.GetInt(CoinKey, 0);
            }
            else if (key == LifeKey)
            {
                userData.life.liveAmount = PlayerPrefs.GetInt(LifeKey, GetMaxLives());
                ScheduleNextLifeIfNeeded();
                PersistLife();
            }
        }

        private void SetCoin(int value)
        {
            userData.coin = Mathf.Max(0, value);
            PlayerPrefs.SetInt(CoinKey, userData.coin);
            PlayerPrefs.Save();
            RaiseUserChanged();
        }

        private void ScheduleNextLifeIfNeeded()
        {
            if (userData.life.liveAmount < GetMaxLives() && userData.life.nextRefillUnixTime <= 0)
            {
                userData.life.nextRefillUnixTime = GetUnixTimeSeconds() + GetRefillInterval();
            }
        }

        private void PersistLife()
        {
            PlayerPrefs.SetInt(LifeKey, userData.life.liveAmount);
            PlayerPrefs.SetString("life_next_refill", userData.life.nextRefillUnixTime.ToString());
            PlayerPrefs.SetString("life_infinity_end", userData.life.liveInfinityEndUnixTime.ToString());
            PlayerPrefs.Save();
        }

        private int GetMaxLives()
        {
            if (ConfigController.Instance != null && ConfigController.Instance.GameConfig != null)
            {
                return Mathf.Max(1, ConfigController.Instance.GameConfig.maxLife);
            }

            return LifeData.MAX_LIVES;
        }

        private int GetRefillInterval()
        {
            int configured = GetParam<int>(TimeToAddLifeKey);
            if (configured > 0)
            {
                return configured;
            }

            if (ConfigController.Instance != null && ConfigController.Instance.GameConfig != null)
            {
                return Mathf.Max(1, ConfigController.Instance.GameConfig.refillLifeTime);
            }

            return 1800;
        }

        private static long GetUnixTimeSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private static string GetBoosterKey(BoosterType boosterType)
        {
            return boosterType switch
            {
                BoosterType.Booster1 => "booster_1",
                BoosterType.Booster2 => "booster_2",
                BoosterType.Booster3 => "booster_3",
                _ => $"booster_{(int)boosterType + 1}"
            };
        }

        private void RaiseUserChanged()
        {
            OnUserChanged.Invoke(userData);
        }
    }
}
