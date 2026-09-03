using Thinh.Base.UI;
using UnityEngine;

public class UIRetryButton : MonoBehaviour
{
    [SerializeField] private UIBaseButton button;

    private void OnEnable()
    {
        button.onClick.AddListener(OnClickRetry);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClickRetry);
    }

    private void OnClickRetry()
    {
        UISceneController.Instance.ReloadCurrentScene();
    }
}
