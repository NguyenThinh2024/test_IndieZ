using System;
using System.Collections.Generic;
using UnityEngine;

namespace Thinh.Base.Gameplay
{
    public enum FailType
    {
        TimeUp,
        OutOfSpace,
        Bomb,
        FailType_4
    }

    [Serializable]
    public class FailTypeConfig
    {
        public FailType failType;

        [Header("Lose")]
        public Sprite loseSprite;
        public string loseText = "TIME UP";

        [Header("Revive")]
        public Sprite reviveSprite;
        public string reviveTitle = "TIME UP";
        [TextArea]
        public string reviveDescription = "Add 30 seconds to keep playing";
    }

    [CreateAssetMenu(fileName = "FailTypeSO", menuName = "Thinh/Gameplay/Fail Type Config")]
    public class FailTypeSO : ScriptableObject
    {
        [SerializeField] private List<FailTypeConfig> configs = new();

        public IReadOnlyList<FailTypeConfig> Configs => configs;

        public FailTypeConfig GetConfig(FailType failType)
        {
            for (int i = 0; i < configs.Count; i++)
            {
                var cfg = configs[i];
                if (cfg != null && cfg.failType == failType)
                    return cfg;
            }

            return null;
        }
    }
}