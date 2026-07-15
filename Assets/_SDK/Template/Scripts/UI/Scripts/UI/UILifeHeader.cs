using UnityEngine;
using TMPro; // dùng TextMeshPro
using Nexzap.Base.Data;
using Nexzap.Base.UI;

namespace Nexzap.Base.UI.Template
{
    public class UILifeHeader : MonoBehaviour
    {
        [SerializeField] private GameObject infinityGo;
        [SerializeField] private UIBaseButton plusBtn;

        [SerializeField] private TextMeshProUGUI liveText;
        [SerializeField] private TextMeshProUGUI timerText;

        private void Start()
        {
            FetchData(null);
        }

        private void OnEnable()
        {
            UserProfileController.Instance.OnUserChanged.AddListener(FetchData);
            plusBtn.onClick.AddListener(OnClickPlus);
        }

        private void OnDisable()
        {
            UserProfileController.Instance?.OnUserChanged?.RemoveListener(FetchData);
            plusBtn?.onClick?.RemoveListener(OnClickPlus);
        }

        private void FetchData(object data)
        {
            UserProfileController.Instance.UpdateLives();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var user = UserProfileController.Instance;

            if (user.HasLiveInfinity)
            {
                // Hiển thị Infinity
                infinityGo?.SetActive(true);
                liveText.text = "∞";
                timerText.text = GameUtils.ConvertRemainTimeMMSS(user.LiveInfinityRemainingSeconds);

                // Infinity thì không cho refill
                plusBtn?.gameObject.SetActive(false);
            }
            else
            {
                infinityGo?.SetActive(false);

                int lives = user.userData.life.liveAmount;
                liveText.text = $"{lives}";

                if (lives < LifeData.MAX_LIVES)
                {
                    int remain = user.NextRefillRemainSeconds;
                    timerText.text = GameUtils.ConvertRemainTimeMMSS(remain);

                    plusBtn?.gameObject.SetActive(true); // hiện nút mua refill
                }
                else
                {
                    timerText.text = "FULL";
                    plusBtn?.gameObject.SetActive(false); // đủ mạng thì ẩn nút mua refill
                }
            }
        }

        private void OnClickPlus()
        {
            AudioController.Instance.PlaySound(SoundName.UI_ButtonClick);
            UIPopupController.Instance.GetActivePopup<UIPopupGetLives>();
        }
    }
}
