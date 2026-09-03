using Thinh.Template;
using UnityEngine;
using Thinh.Base;
namespace Thinh.Template
{
    [CreateAssetMenu(fileName = "RewardSpriteListSO", menuName = "Thinh/Reward Sprite List SO")]
    public class RWSpriteListSO : ScriptableObject
    {
        public RewardSpriteSO[] rewardSpriteSOs;
    }
}

