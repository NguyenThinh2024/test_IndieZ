using Nexzap.Template;
using UnityEngine;
using Nexzap.Base;
namespace Nexzap.Template
{
    [CreateAssetMenu(fileName = "RewardSpriteListSO", menuName = "Nexzap/Reward Sprite List SO")]
    public class RWSpriteListSO : ScriptableObject
    {
        public RewardSpriteSO[] rewardSpriteSOs;
    }
}

