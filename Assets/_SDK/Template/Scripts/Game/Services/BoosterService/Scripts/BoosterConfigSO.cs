using System;
using System.Collections.Generic;
using UnityEngine;
using Thinh.Base.Data;

[Serializable]
public class BoosterConfig
{
    [Header("Basic Info")]
    public BoosterType boosterType;
    public string boosterName;
    public string guideDsc;
    public Sprite icon;
    public BoosterUseMode useMode = BoosterUseMode.Instant;

    [Header("Unlock")]
    public int unlockLevel = 1;
    public int unlockAmount = 0;

    [Header("Economy")]
    public int price = 0;

    [Header("Cooldown (seconds)")]
    [Tooltip("Cooldown riêng cho booster này. 0 = không có cooldown riêng.")]
    public float cooldownSeconds = 0f;
}

public enum BoosterUseMode
{
    Instant,
    Interactive
}

[CreateAssetMenu(fileName = "BoosterConfig", menuName = "Thinh/BoosterConfig")]
public class BoosterConfigSO : ScriptableObject
{
    [SerializeField] public List<BoosterConfig> boosters = new List<BoosterConfig>();

    private Dictionary<BoosterType, BoosterConfig> _cache;

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _cache = new Dictionary<BoosterType, BoosterConfig>(boosters.Count);
        foreach (var cfg in boosters)
        {
            if (cfg == null) continue;
            _cache[cfg.boosterType] = cfg;
        }
    }

    public BoosterConfig GetConfig(BoosterType type)
    {
        if (_cache == null) BuildCache();
        return _cache.TryGetValue(type, out var cfg) ? cfg : null;
    }

    public bool TryGetConfig(BoosterType type, out BoosterConfig cfg)
    {
        cfg = GetConfig(type);
        return cfg != null;
    }
}