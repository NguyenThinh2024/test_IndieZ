using TMPro;
using UnityEngine;
using Thinh.Base.Gameplay;
using Thinh.Base.Level;
using Thinh.Base.Data; // LevelService
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UILevel : MonoBehaviour
{
    [Header("Data Source")]
    [FormerlySerializedAs("gameplaySceneManagerSource")]
    [SerializeField] private MonoBehaviour levelDataSource;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI difficultyTypeText;
    [SerializeField] private Image difficultyImage;

    [Header("Difficulty Colors")]
    [SerializeField] private Color easyColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color hardColor = new Color32(143, 70, 219, 255);
    [SerializeField] private Color supperHardColor = new Color32(76, 18, 27, 255);

    private LevelService levelService;
    private bool isSubscribedToLevelService;
    private ILevelRuntime gameplaySceneManager;
    private ILevelRuntime subscribedGameplaySceneManager;

    private void OnEnable()
    {
        ResolveAndRefresh();
    }

    private void Start()
    {
        ResolveAndRefresh();
    }

    private void OnDisable()
    {
        if (levelService != null && isSubscribedToLevelService)
        {
            levelService.OnLevelLoaded -= HandleLevelLoaded;
        }

        SubscribeGameplaySceneManager(false);
        isSubscribedToLevelService = false;
    }

    private void ResolveAndRefresh()
    {
        ResolveAndSubscribeGameplaySceneManager();
        ResolveAndSubscribeLevelService();
        RefreshUI();
    }

    public void Load()
    {
        ResolveAndRefresh();
    }

    private void RefreshUI()
    {
        int level = ResolveLevelNumber();
        if (levelText != null)
        {
            levelText.text = $"Level {level}";
        }

        ApplyDifficulty(ResolveDifficultyType());
    }

    private void ResolveAndSubscribeLevelService()
    {
        if (levelService == null &&
            GameController.TryGetInstance(out GameController gameController) &&
            gameController.Services != null)
        {
            gameController.Services.TryGet(out levelService);
        }

        if (levelService == null || isSubscribedToLevelService)
        {
            return;
        }

        levelService.OnLevelLoaded += HandleLevelLoaded;
        isSubscribedToLevelService = true;
    }

    private void ResolveAndSubscribeGameplaySceneManager()
    {
        ILevelRuntime resolvedManager = levelDataSource as ILevelRuntime;
        if (resolvedManager == null)
        {
            resolvedManager = FindGameplaySceneManager();
        }

        if (resolvedManager == null)
        {
            return;
        }

        if (ReferenceEquals(gameplaySceneManager, resolvedManager))
        {
            if (subscribedGameplaySceneManager == null)
            {
                SubscribeGameplaySceneManager(true);
            }

            return;
        }

        SubscribeGameplaySceneManager(false);
        gameplaySceneManager = resolvedManager;
        levelDataSource = resolvedManager as MonoBehaviour;
        SubscribeGameplaySceneManager(true);
    }

    private void SubscribeGameplaySceneManager(bool subscribe)
    {
        if (subscribe)
        {
            if (gameplaySceneManager == null)
            {
                return;
            }

            if (subscribedGameplaySceneManager != null &&
                !ReferenceEquals(subscribedGameplaySceneManager, gameplaySceneManager))
            {
                subscribedGameplaySceneManager.LevelLoaded -= HandleGameplayLevelLoaded;
            }

            gameplaySceneManager.LevelLoaded -= HandleGameplayLevelLoaded;
            gameplaySceneManager.LevelLoaded += HandleGameplayLevelLoaded;
            subscribedGameplaySceneManager = gameplaySceneManager;
            return;
        }

        if (subscribedGameplaySceneManager == null)
        {
            return;
        }

        subscribedGameplaySceneManager.LevelLoaded -= HandleGameplayLevelLoaded;
        subscribedGameplaySceneManager = null;
    }

    private int ResolveLevelNumber()
    {
        if (gameplaySceneManager != null)
        {
            return Mathf.Max(1, gameplaySceneManager.GetCurrentLevelIndexPublic() + 1);
        }

        if (levelService != null && levelService.CurrentLevelNumber > 0)
        {
            return levelService.CurrentLevelNumber;
        }

        return UserProfileController.Instance != null ? UserProfileController.Instance.LEVEL : 1;
    }

    private LevelDifficultyType ResolveDifficultyType()
    {
        if (gameplaySceneManager != null)
        {
            return gameplaySceneManager.GetCurrentLevelDifficultyType();
        }

        if (levelService != null && levelService.CurrentLevel != null)
        {
            return levelService.CurrentLevel.difficultyType;
        }

        return LevelDifficultyType.Normal;
    }

    private void ApplyDifficulty(LevelDifficultyType difficultyType)
    {
        Color difficultyColor = GetDifficultyColor(difficultyType);
        if (difficultyImage != null)
        {
            difficultyImage.color = difficultyColor;
        }

        if (difficultyTypeText != null)
        {
            difficultyTypeText.text = GetDifficultyDisplayName(difficultyType);
        }
    }

    private Color GetDifficultyColor(LevelDifficultyType difficultyType)
    {
        switch (difficultyType)
        {
            case LevelDifficultyType.Easy:
                return easyColor;
            case LevelDifficultyType.Hard:
                return hardColor;
            case LevelDifficultyType.SupperHard:
                return supperHardColor;
            case LevelDifficultyType.Normal:
            default:
                return normalColor;
        }
    }

    private static string GetDifficultyDisplayName(LevelDifficultyType difficultyType)
    {
        switch (difficultyType)
        {
            case LevelDifficultyType.Easy:
                return "Easy";
            case LevelDifficultyType.Hard:
                return "Hard";
            case LevelDifficultyType.SupperHard:
                return "Supper Hard";
            case LevelDifficultyType.Normal:
            default:
                return "Normal";
        }
    }

    private void HandleLevelLoaded(int levelNumber, LevelConfig levelConfig)
    {
        RefreshUI();
    }

    private void HandleGameplayLevelLoaded()
    {
        RefreshUI();
    }

    private static ILevelRuntime FindGameplaySceneManager()
    {
#if UNITY_2023_1_OR_NEWER
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
#endif
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ILevelRuntime manager)
            {
                return manager;
            }
        }

        return null;
    }
}
