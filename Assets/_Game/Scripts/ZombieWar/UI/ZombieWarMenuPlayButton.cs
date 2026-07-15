using Nexzap.Base;
using Nexzap.Base.Data;
using Nexzap.Base.UI;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Level;

namespace ZombieWar.UI
{
    /// <summary>
    /// Menu Play button: sync LEVEL → Gameplay.
    /// First play uses level 1 from profile; after win/next, Play continues current LEVEL.
    /// </summary>
    public sealed class ZombieWarMenuPlayButton : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Text label;
        [SerializeField] private bool consumeLife;
        [SerializeField] private bool forceStartLevel1OnFirstPlay;

        private void Awake()
        {
            if (playButton == null)
            {
                playButton = GetComponent<Button>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<Text>(true);
            }
        }

        private void OnEnable()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (UserProfileController.Instance != null)
            {
                UserProfileController.Instance.OnUserChanged.AddListener(OnUserChanged);
            }

            refreshLabel();
        }

        private void OnDisable()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (UserProfileController.Instance != null)
            {
                UserProfileController.Instance.OnUserChanged.RemoveListener(OnUserChanged);
            }
        }

        private void OnUserChanged(object _)
        {
            refreshLabel();
        }

        private void refreshLabel()
        {
            if (label == null)
            {
                return;
            }

            int level = resolveLevel();
            label.text = $"PLAY — LEVEL {level}";
        }

        private void OnPlayClicked()
        {
            int level = resolveLevel();
            if (forceStartLevel1OnFirstPlay && !PlayerPrefs.HasKey(HasPlayedKey))
            {
                level = 1;
            }

            applyLevel(level);
            PlayerPrefs.SetInt(HasPlayedKey, 1);
            PlayerPrefs.Save();

            if (consumeLife && UserProfileController.Instance != null)
            {
                UserProfileController.Instance.UseLife();
            }

            GameDataHelper.PlayType = "menu";

            if (UISceneController.Instance != null)
            {
                UISceneController.Instance.ChangeScene(SceneName.Gameplay);
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Gameplay);
        }

        private static int resolveLevel()
        {
            if (PlayerPrefs.HasKey("ZW_SessionLevel"))
            {
                return Mathf.Max(1, PlayerPrefs.GetInt("ZW_SessionLevel", 1));
            }

            UserProfileController profile = UserProfileController.Instance;
            if (profile != null)
            {
                return Mathf.Max(1, profile.LEVEL);
            }

            return 1;
        }

        private static void applyLevel(int level)
        {
            level = Mathf.Max(1, level);
            UserProfileController profile = UserProfileController.Instance;
            if (profile != null)
            {
                profile.LEVEL = level;
            }

            LevelMapBootstrap.PersistSessionLevel(level);
        }

        private const string HasPlayedKey = "ZW_HasPlayedFromMenu";
    }
}
