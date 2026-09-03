using Thinh.Base.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thinh.Template
{
    public class UIRewardIcon : MonoBehaviour
    {

        [SerializeField] private RewardSpriteSO rewardSpriteSO;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI amountText;

        public void Parse(RewardType rewardType, int amount)
        {
            icon.sprite = rewardSpriteSO.GetSprite(rewardType);
            amountText.text = rewardType == RewardType.LiveInfinity ? $"{amount}m" : $"{amount}";
        }
    }
}

