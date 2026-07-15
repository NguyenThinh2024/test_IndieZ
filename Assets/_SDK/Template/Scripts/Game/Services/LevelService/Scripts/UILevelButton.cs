using DG.Tweening;
using Nexzap.Base.Data;
using Nexzap.Base.UI;
using Nexzap.Base.UI.Template;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

namespace Nexzap.Base.Level
{
    public class UILevelButton : MonoBehaviour
    {
        [SerializeField]
        private UIBaseButton button;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI levelText;   // hiện số level

        private bool isInited;
        private int currentLevel;

        private void OnEnable()
        {
            button.onClick.AddListener(OnClickPlay);
            UserProfileController.Instance.OnUserChanged.AddListener(RefreshUI);
        }

        private void OnDisable()
        {
            button?.onClick?.RemoveListener(OnClickPlay);
            UserProfileController.Instance?.OnUserChanged?.RemoveListener(RefreshUI);
        }

        private void Start()
        {
            isInited = false;
            RefreshUI();
            isInited = true;
        }

        private async void RefreshUI(object data=null)
        {
            var levelIndex = UserProfileController.Instance.LEVEL;

            var isAnimation = isInited && currentLevel != levelIndex;

            if (isAnimation) await transform.DOScale(Vector3.one * 1.5f, 0.3f).AsyncWaitForCompletion();

            // Hiển thị số level
            if (levelText != null)
                levelText.text = $"Level {(levelIndex + 1)}";

            if (isAnimation) transform.DOScale(Vector3.one, 0.1f);
            currentLevel = levelIndex;
        }

        private void OnClickPlay()
        {
            if (UserProfileController.Instance.UseLife())
            {
                UISceneController.Instance.ChangeScene(SceneName.Gameplay);
            }
            else
            {
                UIPopupController.Instance.GetActivePopup<UIPopupGetLives>();
            }
        }
    }

}
