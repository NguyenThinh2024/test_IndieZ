using Thinh.Base.UI;
using Thinh.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;
using Thinh.Base.Gameplay;

public class UIGameSettingPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot; // panel gốc (nền + nội dung)
    [SerializeField] private RectTransform content; // phần nội dung chính (không tắt khi click vào)

    [Header("Sound UI")]
    [SerializeField] private UIBaseButton soundBtn;
    [SerializeField] private GameObject soundOnGo;
    [SerializeField] private GameObject soundOffGo;

    [Header("Music UI")]
    [SerializeField] private UIBaseButton musicBtn;
    [SerializeField] private GameObject musicOnGo;
    [SerializeField] private GameObject musicOffGo;

    [Header("Vibration UI")]
    [SerializeField] private UIBaseButton vibrationBtn;
    [SerializeField] private GameObject vibrationOnGo;
    [SerializeField] private GameObject vibrationOffGo;

    [Header("Game UI")]
    [SerializeField] private UIBaseButton restartBtn;
    [SerializeField] private UIBaseButton exitBtn;

    [Header("Animation")]
    [SerializeField] private float duration = 0.3f;
    private Vector2 shownPos;
    private Vector2 hiddenPos;

    private bool isShown = false;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // Lưu vị trí "show" của content (vị trí gốc trong canvas)
        shownPos = content.anchoredPosition;

        // Xác định vị trí "ẩn" (dịch sang phải ngoài màn hình)
        hiddenPos = new Vector2(Screen.width, shownPos.y);
        content.anchoredPosition = hiddenPos;
    }

    private void OnEnable()
    {
        soundBtn.onClick.AddListener(OnClickSound);
        musicBtn.onClick.AddListener(OnClickMusic);
        vibrationBtn.onClick.AddListener(OnClickVibration);
        restartBtn.onClick.AddListener(OnClickRestart);
        exitBtn.onClick.AddListener(OnClickQuit);

        RefreshUI();
    }

    private void OnDisable()
    {
        soundBtn?.onClick?.RemoveListener(OnClickSound);
        musicBtn?.onClick?.RemoveListener(OnClickMusic);
        vibrationBtn?.onClick?.RemoveListener(OnClickVibration);
        restartBtn?.onClick?.RemoveListener(OnClickRestart);
        exitBtn?.onClick?.RemoveListener(OnClickQuit);
    }

    private void RefreshUI()
    {
        // Sound
        bool soundOn = !AudioController.Instance.IsMuteSound;
        soundOnGo.SetActive(soundOn);
        soundOffGo.SetActive(!soundOn);

        // Music
        bool musicOn = !AudioController.Instance.IsMuteMusic;
        musicOnGo.SetActive(musicOn);
        musicOffGo.SetActive(!musicOn);

        // Vibration
        bool vibOn = !VibrationController.Instance.IsMuteVibration;
        vibrationOnGo.SetActive(vibOn);
        vibrationOffGo.SetActive(!vibOn);
    }

    private void OnClickSound()
    {
        AudioController.Instance.ToggleSound();
        RefreshUI();
    }

    private void OnClickMusic()
    {
        AudioController.Instance.ToggleMusic();
        RefreshUI();
    }

    private void OnClickVibration()
    {
        VibrationController.Instance.ToggleVibration();
        RefreshUI();
    }

    private void OnClickRestart()
    {
        UIPopupController.Instance.GetActivePopup<UIGameRestartPopup>();
        Hide();
    }


    private void OnClickQuit()
    {
        UIPopupController.Instance.GetActivePopup<UIGameQuitPopup>();
        Hide();
    }

    /// <summary>
    /// Bật Panel với animation
    /// </summary>
    public void Show()
    {
        if (isShown) return;

        isShown = true;
        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshUI();

        content.DOKill();
        content.anchoredPosition = hiddenPos; // đảm bảo start từ phải
        content.DOAnchorPos(shownPos, duration).SetEase(Ease.OutCubic);

        GameController.Instance.Services.Get<GameStateService>().Pause();
    }

    /// <summary>
    /// Tắt Panel với animation
    /// </summary>
    public void Hide()
    {
        content.DOKill();
        content.DOAnchorPos(hiddenPos, duration)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                isShown = false;
            });

        GameController.Instance.Services.Get<GameStateService>().Play();
    }

    private void Update()
    {
        if (!isShown) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (!RectTransformUtility.RectangleContainsScreenPoint(content, mousePos))
            {
                Hide();
            }
        }
    }
}
