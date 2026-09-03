using System.Collections;
using Thinh.Base;
using Thinh.Base.Data;
using Thinh.Base.Gameplay;
using Thinh.Base.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using UnityEngine.UI;

namespace Thinh.Template
{
    public class CheatCanvas : MonoSingleton<CheatCanvas>
    {
        [Header("UI")]
        [SerializeField] private GameObject cheatRoot;
        [SerializeField] private UIBaseButton prevBtn;
        [SerializeField] private TMP_InputField levelInput;
        [SerializeField] private UIBaseButton loadBtn;
        [SerializeField] private UIBaseButton nextBtn;
        [SerializeField] private UIBaseButton winBtn;
        [SerializeField] private UIBaseButton failBtn;
        [SerializeField] private UIBaseButton coinBtn;
        [SerializeField] private Toggle onboardingToggle;

        [Header("Gesture - Editor Mouse (5 clicks in 1s)")]
        [SerializeField] private int requiredClicks = 5;
        [SerializeField] private float windowSeconds = 1.0f;

        private int _clickCount;
        private float _firstClickTime = -1f;
        private TMP_Text _onboardingToggleLabel;

        private IEnumerator Start()
        {
            if (cheatRoot != null) cheatRoot.SetActive(false);

            EnsureOnboardingToggle();
            SyncOnboardingToggle(OnboardingRuntimeSettings.IsEnabled);
            OnboardingRuntimeSettings.EnabledChanged += HandleOnboardingEnabledChanged;

            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(3);

            prevBtn.onClick.AddListener(() =>
            {
                UserProfileController.Instance.LEVEL -= 1;
                persistAndReloadLevel();
            });

            nextBtn.onClick.AddListener(() =>
            {
                UserProfileController.Instance.LEVEL += 1;
                persistAndReloadLevel();
            });

            loadBtn.onClick.AddListener(() =>
            {
                if (int.TryParse(levelInput.text, out int lv))
                {
                    UserProfileController.Instance.LEVEL = lv;
                    persistAndReloadLevel();
                }
            });

            winBtn.onClick.AddListener(forceWin);
            failBtn.onClick.AddListener(forceFail);
            coinBtn.onClick.AddListener(() => { UserProfileController.Instance.AddCoin(1000); });

            UserProfileController.Instance.OnUserChanged.AddListener(FetchData);
            FetchData(null);
        }

        private static void persistAndReloadLevel()
        {
            int level = Mathf.Max(1, UserProfileController.Instance.LEVEL);
            ZombieWar.Level.LevelMapBootstrap.PersistSessionLevel(level);
            Time.timeScale = 1f;

            if (UISceneController.Instance != null)
            {
                UISceneController.Instance.ChangeScene(SceneName.Gameplay);
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Gameplay);
        }

        private static void forceWin()
        {
            Time.timeScale = 1f;

            // ZombieWar Gameplay first — template GameController may exist DontDestroyOnLoad
            // and would swallow the cheat without showing ZW win panel.
            ZombieWar.Level.ZombieWarGameFlow flow =
                Object.FindFirstObjectByType<ZombieWar.Level.ZombieWarGameFlow>();
            if (flow != null)
            {
                flow.ForceWin();
                return;
            }

            tryTemplateForceWin();
        }

        private static void forceFail()
        {
            Time.timeScale = 1f;

            ZombieWar.Level.ZombieWarGameFlow flow =
                Object.FindFirstObjectByType<ZombieWar.Level.ZombieWarGameFlow>();
            if (flow != null)
            {
                flow.ForceLose();
                return;
            }

            tryTemplateForceFail();
        }

        private static void tryTemplateForceWin()
        {
            GameController controller = GameController.Instance;
            if (controller == null || controller.Services == null)
            {
                return;
            }

            GameResultHandleService resultHandleService = controller.Services.Get<GameResultHandleService>();
            resultHandleService?.ForceShowWin();
        }

        private static void tryTemplateForceFail()
        {
            GameController controller = GameController.Instance;
            if (controller == null || controller.Services == null)
            {
                return;
            }

            GameStateService stateService = controller.Services.Get<GameStateService>();
            stateService?.Lose(FailType.TimeUp);
        }

        private void OnDestroy()
        {
            OnboardingRuntimeSettings.EnabledChanged -= HandleOnboardingEnabledChanged;

            if (onboardingToggle != null)
            {
                onboardingToggle.onValueChanged.RemoveListener(HandleOnboardingToggleValueChanged);
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                RegisterClick();
            }
        }

        private void RegisterClick()
        {
            float now = Time.unscaledTime;

            // start window
            if (_firstClickTime < 0f)
            {
                _firstClickTime = now;
                _clickCount = 1;
                return;
            }

            // if window expired => restart window from this click
            if (now - _firstClickTime > windowSeconds)
            {
                _firstClickTime = now;
                _clickCount = 1;
                return;
            }

            // still inside window
            _clickCount++;

            if (_clickCount >= requiredClicks)
            {
                ToggleCheat();
                ResetWindow();
            }
        }

        private void ResetWindow()
        {
            _clickCount = 0;
            _firstClickTime = -1f;
        }

        private void ToggleCheat()
        {
            if (cheatRoot != null)
                cheatRoot.SetActive(!cheatRoot.activeSelf);
            else
                gameObject.SetActive(!gameObject.activeSelf);
        }

        private void HandleOnboardingEnabledChanged(bool isEnabled)
        {
            SyncOnboardingToggle(isEnabled);
        }

        private void HandleOnboardingToggleValueChanged(bool isOn)
        {
            OnboardingRuntimeSettings.IsEnabled = isOn;
            UpdateOnboardingToggleLabel(isOn);
        }

        private void SyncOnboardingToggle(bool isEnabled)
        {
            if (onboardingToggle == null)
            {
                return;
            }

            onboardingToggle.SetIsOnWithoutNotify(isEnabled);
            UpdateOnboardingToggleLabel(isEnabled);
        }

        private void EnsureOnboardingToggle()
        {
            if (cheatRoot == null)
            {
                return;
            }

            if (onboardingToggle == null)
            {
                onboardingToggle = CreateOnboardingToggleRuntime();
            }

            if (onboardingToggle == null)
            {
                return;
            }

            onboardingToggle.onValueChanged.RemoveListener(HandleOnboardingToggleValueChanged);
            onboardingToggle.onValueChanged.AddListener(HandleOnboardingToggleValueChanged);
        }

        private Toggle CreateOnboardingToggleRuntime()
        {
            RectTransform cheatTransform = cheatRoot.transform as RectTransform;
            if (cheatTransform == null)
            {
                return null;
            }

            TMP_FontAsset fontAsset = ResolveToggleFontAsset();

            GameObject rowObject = new GameObject(
                "onboarding toggle row",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(cheatTransform, false);

            Image rowBackground = rowObject.GetComponent<Image>();
            rowBackground.color = new Color(1f, 1f, 1f, 0.92f);
            rowBackground.raycastTarget = false;

            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.minHeight = 90f;
            rowLayout.preferredHeight = 90f;

            HorizontalLayoutGroup rowGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowGroup.padding = new RectOffset(24, 24, 12, 12);
            rowGroup.spacing = 16f;
            rowGroup.childAlignment = TextAnchor.MiddleCenter;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = true;

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            labelObject.transform.SetParent(rowObject.transform, false);

            LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;

            TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.font = fontAsset;
            labelText.fontSize = 28f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = new Color32(50, 50, 50, 255);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.raycastTarget = false;
            _onboardingToggleLabel = labelText;

            GameObject toggleObject = new GameObject(
                "Toggle",
                typeof(RectTransform),
                typeof(Toggle),
                typeof(LayoutElement));
            toggleObject.transform.SetParent(rowObject.transform, false);

            LayoutElement toggleLayout = toggleObject.GetComponent<LayoutElement>();
            toggleLayout.minWidth = 72f;
            toggleLayout.preferredWidth = 72f;

            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(72f, 48f);

            GameObject backgroundObject = new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            backgroundObject.transform.SetParent(toggleObject.transform, false);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = new Color32(42, 42, 42, 255);

            GameObject checkmarkObject = new GameObject(
                "Checkmark",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            checkmarkObject.transform.SetParent(backgroundObject.transform, false);

            RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;

            TextMeshProUGUI checkmarkText = checkmarkObject.GetComponent<TextMeshProUGUI>();
            checkmarkText.font = fontAsset;
            checkmarkText.text = "✓";
            checkmarkText.fontSize = 30f;
            checkmarkText.fontStyle = FontStyles.Bold;
            checkmarkText.color = new Color32(112, 201, 65, 255);
            checkmarkText.alignment = TextAlignmentOptions.Center;
            checkmarkText.raycastTarget = false;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkText;

            LayoutRebuilder.ForceRebuildLayoutImmediate(cheatTransform);
            return toggle;
        }

        private TMP_FontAsset ResolveToggleFontAsset()
        {
            if (cheatRoot != null)
            {
                TMP_Text existingText = cheatRoot.GetComponentInChildren<TMP_Text>(true);
                if (existingText != null && existingText.font != null)
                {
                    return existingText.font;
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        private void UpdateOnboardingToggleLabel(bool isEnabled)
        {
            if (_onboardingToggleLabel != null)
            {
                _onboardingToggleLabel.text = $"Onboarding: {(isEnabled ? "ON" : "OFF")}";
            }
        }

        private void FetchData(object data)
        {
            levelInput.text = UserProfileController.Instance.LEVEL.ToString();
        }
    }
}
