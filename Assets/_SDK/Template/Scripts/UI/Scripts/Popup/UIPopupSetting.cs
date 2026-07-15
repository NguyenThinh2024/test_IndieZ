using UnityEngine;
using Nexzap.Base.UI;
using TMPro;

namespace Nexzap.Base.UI.Template
{
    public class UIPopupSetting : UIBasePopup
    {
        private const float SliderToggleDuration = 0.125f;

        [Header("Sound UI")]
        [SerializeField] private UIBaseButton soundBtn;
        [SerializeField] private GameObject soundOnGo;
        [SerializeField] private GameObject soundOffGo;
        [SerializeField] private SliderLinearValueHandler soundSliderValueHandler;

        [Header("Music UI")]
        [SerializeField] private UIBaseButton musicBtn;
        [SerializeField] private GameObject musicOnGo;
        [SerializeField] private GameObject musicOffGo;
        [SerializeField] private SliderLinearValueHandler musicSliderValueHandler;

        [Header("Vibration UI")]
        [SerializeField] private UIBaseButton vibrationBtn;
        [SerializeField] private GameObject vibrationOnGo;
        [SerializeField] private GameObject vibrationOffGo;

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI versionText;

        private void OnEnable()
        {
            ResolveSliderValueHandlers();

            soundBtn.onClick.AddListener(OnClickSound);
            musicBtn.onClick.AddListener(OnClickMusic);
            vibrationBtn.onClick.AddListener(OnClickVibration);

            RefreshUI();
        }

        private void OnDisable()
        {
            soundBtn?.onClick?.RemoveListener(OnClickSound);
            musicBtn?.onClick?.RemoveListener(OnClickMusic);
            vibrationBtn?.onClick?.RemoveListener(OnClickVibration);
        }

        public override void Show()
        {
            base.Show();
            ResolveSliderValueHandlers();
            RefreshUI();
        }

        private void OnValidate()
        {
            ResolveSliderValueHandlers();
        }

        private void RefreshUI(bool animateSlider = false)
        {
            ResolveSliderValueHandlers();

            // Sound
            bool soundOn = !AudioController.Instance.IsMuteSound;
            soundOnGo.SetActive(soundOn);
            soundOffGo.SetActive(!soundOn);
            ApplySliderValue(soundSliderValueHandler, soundOn, animateSlider);

            // Music
            bool musicOn = !AudioController.Instance.IsMuteMusic;
            musicOnGo.SetActive(musicOn);
            musicOffGo.SetActive(!musicOn);
            ApplySliderValue(musicSliderValueHandler, musicOn, animateSlider);

            // Vibration
            bool vibOn = !VibrationController.Instance.IsMuteVibration;
            vibrationOnGo.SetActive(vibOn);
            vibrationOffGo.SetActive(!vibOn);

            versionText.text = $"version {Application.version}";
        }


        private void OnClickSound()
        {
            AudioController.Instance.ToggleSound();
            RefreshUI(true);
        }

        private void OnClickMusic()
        {
            AudioController.Instance.ToggleMusic();
            RefreshUI(true);
        }

        private void OnClickVibration()
        {
            VibrationController.Instance.ToggleVibration();
            RefreshUI();
        }

        private static void ApplySliderValue(
            SliderLinearValueHandler sliderValueHandler,
            bool isOn,
            bool animateSlider)
        {
            if (sliderValueHandler == null)
            {
                return;
            }

            float targetValue = isOn ? 1f : 0f;
            if (animateSlider)
            {
                sliderValueHandler.SetValue(targetValue, SliderToggleDuration);
                return;
            }

            sliderValueHandler.SetValueInstant(targetValue);
        }

        private void ResolveSliderValueHandlers()
        {
            if (soundSliderValueHandler == null && soundBtn != null)
            {
                soundSliderValueHandler = soundBtn.GetComponentInChildren<SliderLinearValueHandler>(true);
            }

            if (musicSliderValueHandler == null && musicBtn != null)
            {
                musicSliderValueHandler = musicBtn.GetComponentInChildren<SliderLinearValueHandler>(true);
            }
        }
    }
}
