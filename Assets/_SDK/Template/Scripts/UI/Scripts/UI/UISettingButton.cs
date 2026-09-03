using UnityEngine;
using Thinh.Base.UI;
using Thinh.Template;
using Thinh.Base.Gameplay; // if you are using UIBasePopup/UIPopupController flow

namespace Thinh.Base.UI.Template
{
    public class UISettingButton : MonoBehaviour
    {
        [SerializeField] private UIBaseButton settingBtn;

        private void OnEnable()
        {
            settingBtn.onClick.AddListener(OnClickSetting);
        }

        private void OnDisable()
        {
            settingBtn?.onClick?.RemoveListener(OnClickSetting);
        }

        private void OnClickSetting()
        {
            var popup = UIPopupController.Instance.GetActivePopup<UIPopupSetting>();
            if (popup == null)
            {
                return;
            }

            GameController.Instance.Services.Get<GameStateService>().Pause();
            popup.onClosed.RemoveAllListeners();
            popup.onClosed.AddListener(() =>
            {
                GameController.Instance.Services.Get<GameStateService>().Play();
            });
            popup.Show();
        }
    }
}
