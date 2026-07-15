using System;
using Nexzap.Base.Data;
using Nexzap.Base.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieWar.Player;

namespace ZombieWar.Level
{
    /// <summary>
    /// Owns level win/lose and result navigation (Replay / Next Level).
    /// Win: all wave spawns finished and no living targets remain.
    /// Lose: player health depleted (Current &lt;= 0 / Died).
    /// </summary>
    public sealed class ZombieWarGameFlow : MonoBehaviour
    {
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private LevelMapBootstrap levelMapBootstrap;

        [SerializeField] private bool startWavesOnPlay = true;
        [SerializeField] private bool waitForMapBeforeStart = true;

        [SerializeField] private bool pauseTimeOnFinish = true;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private bool autoCreateResultUi = true;

        [SerializeField] private Button winReplayButton;
        [SerializeField] private Button winNextLevelButton;
        [SerializeField] private Button loseReplayButton;

        public event Action Won;
        public event Action Lost;
        public event Action<float> TimeNormalizedChanged;

        public bool IsFinished => finished;
        public bool HasWon { get; private set; }
        public bool HasLost { get; private set; }

        public bool HasNextLevel
        {
            get
            {
                if (levelMapBootstrap == null)
                {
                    return false;
                }

                return levelMapBootstrap.HasNextLevel;
            }
        }

        private bool finished;
        private bool hasBegunGameplay;
        private float cachedTimeScale = 1f;
        private GameObject autoCanvasRoot;

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died += Lose;
            }

            if (waveManager != null)
            {
                waveManager.Cleared += Win;
            }

            if (levelMapBootstrap != null)
            {
                levelMapBootstrap.MapReady += OnMapReady;
            }

            bindResultButtons(true);
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= Lose;
            }

            if (waveManager != null)
            {
                waveManager.Cleared -= Win;
            }

            if (levelMapBootstrap != null)
            {
                levelMapBootstrap.MapReady -= OnMapReady;
            }

            bindResultButtons(false);
            restoreTimeScale();
        }

        private void Start()
        {
            finished = false;
            HasWon = false;
            HasLost = false;
            hasBegunGameplay = false;

            // Re-bind in Start so Cleared is never missed if WaveManager finishes wiring late.
            if (waveManager != null)
            {
                waveManager.Cleared -= Win;
                waveManager.Cleared += Win;
            }

            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }

            if (losePanel != null)
            {
                losePanel.SetActive(false);
            }

            if (waitForMapBeforeStart && levelMapBootstrap != null)
            {
                if (levelMapBootstrap.IsMapReady)
                {
                    beginGameplay();
                }

                // Otherwise beginGameplay runs from MapReady.
                return;
            }

            beginGameplay();
        }

        private void OnMapReady(GameObject _)
        {
            if (finished || !waitForMapBeforeStart)
            {
                return;
            }

            beginGameplay();
        }

        private void beginGameplay()
        {
            if (hasBegunGameplay || finished)
            {
                return;
            }

            hasBegunGameplay = true;

            if (startWavesOnPlay)
            {
                waveManager?.StartWaves();
            }
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            if (waveManager != null)
            {
                TimeNormalizedChanged?.Invoke(waveManager.NormalizedTime);
            }
        }

        public void Replay()
        {
            int level = getCurrentLevelNumber();
            LevelMapBootstrap.PersistSessionLevel(level);
            reloadGameplay(keepLevel: true);
        }

        public void GoToNextLevel()
        {
            int current = getCurrentLevelNumber();
            int next = current + 1;
            if (!HasNextLevel)
            {
                return;
            }

            setProfileLevel(next);
            LevelMapBootstrap.PersistSessionLevel(next);
            reloadGameplay(keepLevel: false);
        }

        public void ForceWin()
        {
            // Cheat can force win even after a prior finish state.
            finished = false;
            HasLost = false;
            if (losePanel != null)
            {
                losePanel.SetActive(false);
            }

            Win();
        }

        public void ForceLose()
        {
            finished = false;
            HasWon = false;
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }

            Lose();
        }

        private void Win()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            HasWon = true;
            HasLost = false;
            waveManager?.StopWaves();
            applyFinishPause();
            showResultPanel(isWin: true);
            Won?.Invoke();
        }

        private void Lose()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            HasLost = true;
            HasWon = false;
            waveManager?.StopWaves();
            applyFinishPause();
            showResultPanel(isWin: false);
            Lost?.Invoke();
        }

        private void showResultPanel(bool isWin)
        {
            if (losePanel != null)
            {
                losePanel.SetActive(!isWin);
            }

            if (winPanel != null)
            {
                winPanel.SetActive(isWin);
            }

            GameObject panel = isWin ? winPanel : losePanel;
            if (panel == null && autoCreateResultUi)
            {
                ensureAutoResultUi();
                panel = isWin ? winPanel : losePanel;
                if (panel != null)
                {
                    panel.SetActive(true);
                }
            }

            if (isWin && winNextLevelButton != null)
            {
                winNextLevelButton.gameObject.SetActive(HasNextLevel);
            }
        }

        private void ensureAutoResultUi()
        {
            if (autoCanvasRoot != null)
            {
                return;
            }

            autoCanvasRoot = new GameObject("ZW_ResultCanvas");
            Canvas canvas = autoCanvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            autoCanvasRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            autoCanvasRoot.AddComponent<GraphicRaycaster>();

            winPanel = createResultPanel(
                autoCanvasRoot.transform,
                "WinPanel",
                "YOU WIN",
                new Color(0.12f, 0.55f, 0.22f, 0.92f),
                includeNext: true,
                out winReplayButton,
                out winNextLevelButton);
            losePanel = createResultPanel(
                autoCanvasRoot.transform,
                "LosePanel",
                "YOU LOSE",
                new Color(0.55f, 0.12f, 0.12f, 0.92f),
                includeNext: false,
                out loseReplayButton,
                out _);

            winPanel.SetActive(false);
            losePanel.SetActive(false);
            bindResultButtons(true);
        }

        private static GameObject createResultPanel(
            Transform parent,
            string name,
            string message,
            Color color,
            bool includeNext,
            out Button replayButton,
            out Button nextButton)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = color;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.50f);
            textRect.anchorMax = new Vector2(0.9f, 0.72f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.text = message;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 64;
            text.font = resolveFont();

            if (includeNext)
            {
                replayButton = createActionButton(panel.transform, "ReplayButton", "REPLAY", new Vector2(0.12f, 0.22f), new Vector2(0.48f, 0.38f));
                nextButton = createActionButton(panel.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(0.52f, 0.22f), new Vector2(0.88f, 0.38f));
            }
            else
            {
                replayButton = createActionButton(panel.transform, "ReplayButton", "REPLAY", new Vector2(0.30f, 0.22f), new Vector2(0.70f, 0.38f));
                nextButton = null;
            }

            return panel;
        }

        private static Button createActionButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text text = labelObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 36;
            text.font = resolveFont();

            return buttonObject.GetComponent<Button>();
        }

        private static Font resolveFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void bindResultButtons(bool bind)
        {
            if (winReplayButton != null)
            {
                if (bind)
                {
                    winReplayButton.onClick.AddListener(Replay);
                }
                else
                {
                    winReplayButton.onClick.RemoveListener(Replay);
                }
            }

            if (winNextLevelButton != null)
            {
                if (bind)
                {
                    winNextLevelButton.onClick.AddListener(GoToNextLevel);
                }
                else
                {
                    winNextLevelButton.onClick.RemoveListener(GoToNextLevel);
                }
            }

            if (loseReplayButton != null)
            {
                if (bind)
                {
                    loseReplayButton.onClick.AddListener(Replay);
                }
                else
                {
                    loseReplayButton.onClick.RemoveListener(Replay);
                }
            }
        }

        private void reloadGameplay(bool keepLevel)
        {
            restoreTimeScale();

            if (!keepLevel)
            {
                // Profile already advanced in GoToNextLevel.
            }

            if (UISceneController.Instance != null)
            {
                UISceneController.Instance.ChangeScene(SceneName.Gameplay);
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private int getCurrentLevelNumber()
        {
            if (TryGetProfileLevel(out int profileLevel))
            {
                return profileLevel;
            }

            return levelMapBootstrap != null ? levelMapBootstrap.LevelNumber : 1;
        }

        private static bool TryGetProfileLevel(out int level)
        {
            level = 1;
            UserProfileController profile = UserProfileController.Instance;
            if (profile == null)
            {
                return false;
            }

            level = Mathf.Max(1, profile.LEVEL);
            return true;
        }

        private static void setProfileLevel(int level)
        {
            UserProfileController profile = UserProfileController.Instance;
            if (profile == null)
            {
                return;
            }

            profile.LEVEL = Mathf.Max(1, level);
        }

        private void applyFinishPause()
        {
            if (!pauseTimeOnFinish)
            {
                return;
            }

            cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void restoreTimeScale()
        {
            if (!pauseTimeOnFinish)
            {
                return;
            }

            Time.timeScale = cachedTimeScale > 0f ? cachedTimeScale : 1f;
        }
    }
}
