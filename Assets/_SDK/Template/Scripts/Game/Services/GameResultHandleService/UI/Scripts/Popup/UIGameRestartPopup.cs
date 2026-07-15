using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Nexzap.Base.UI;
using UnityEngine.Events;
using Nexzap.Base.Ads;
using Nexzap.Base.Data;

public class UIGameRestartPopup : UIBasePopup
{
    [Header("Lose")]
    [SerializeField] private UIBaseButton loseBtn;

    private void OnEnable()
    {
        loseBtn.onClick.AddListener(OnClickLose);
    }

    private void OnDisable()
    {
        loseBtn?.onClick.RemoveListener(OnClickLose);
    }

    private void OnClickLose()
    {
        UserProfileController.Instance.ResetWinStreak();
        UserProfileController.Instance.UseLife();
        UISceneController.Instance.ChangeScene(SceneName.Gameplay);
        UIPopupController.Instance.HidePopup<UIGameRestartPopup>();
    }
}
