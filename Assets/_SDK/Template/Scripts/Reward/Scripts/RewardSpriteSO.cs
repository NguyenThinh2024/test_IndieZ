using System;
using System.Collections.Generic;
using Nexzap.Base.Data;
using UnityEngine;

namespace Nexzap.Template
{

    [CreateAssetMenu(fileName = "RewardSpriteSO", menuName = "Nexzap/Reward Sprite SO", order = 0)]
    public class RewardSpriteSO : ScriptableObject
    {
        [Serializable]
        public class RewardSpriteData
        {
            public RewardType rewardType;
            public Sprite sprite;
            public List<Sprite> animations;
        }

        [SerializeField] private List<RewardSpriteData> rewardSprites = new();

        /// <summary>
        /// Lấy sprite theo RewardType
        /// </summary>
        public Sprite GetSprite(RewardType type)
        {
            return rewardSprites.Find(x=>x.rewardType == type).sprite;
        }
        public RewardType GetRewardType(int index)
        {
            return rewardSprites[index].rewardType;
        }
        public List<RewardSpriteData> GetSpritesList()
        {
            return rewardSprites;
        }
    }
}
