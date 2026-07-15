using Nexzap.Base.Data;
using Nexzap.Base.UI;
using Nexzap.Base.UI.Template;
using Nexzap.Template;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Nexzap.Base.Gameplay
{
    /// <summary>
    /// After all features in FeatureUnlockSO are unlocked, takes over the
    /// Feature Unlock UI (UIFeatureProgress) inside the Win popup to show
    /// recurring chest gift progress. Awards coins on gift levels.
    /// Place on a standalone GO in the Gameplay scene.
    /// </summary>
    public class GiftCycleHandler : MonoBehaviour
    {
        [Header("Gift Config")]
        [SerializeField] private Sprite chestLockedSprite;
        [SerializeField] private Sprite chestUnlockedSprite;
        [SerializeField] [Min(1)] private int giftInterval = 5;
        [SerializeField] private int giftCoinAmount = 10;

        private const string LastGiftClaimedKey = "gift_cycle_last_claimed_level";

        private FeatureUnlockSO _featureSO;
        private GameStateService _gameStateService;
        private bool _initialized;

        private void Update()
        {
            if (_initialized) return;
            if (GameController.Instance == null) return;

            _initialized = true;

            var featureService = GameController.Instance.Services.Get<FeatureUnlockService>();
            if (featureService != null)
            {
                var field = typeof(FeatureUnlockService).GetField(
                    "featureSO",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    _featureSO = field.GetValue(featureService) as FeatureUnlockSO;
            }

            if (_featureSO == null)
            {
                _initialized = false;
                return;
            }

            GameController.Instance.Services.TryGet(out _gameStateService);
            if (_gameStateService != null)
                _gameStateService.OnChangeState += OnGameStateChanged;

            enabled = false;
        }

        private void OnDestroy()
        {
            if (_gameStateService != null)
                _gameStateService.OnChangeState -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState prev, GameState next)
        {
            if (next != GameState.Win) return;

            int currentLevel = UserProfileController.Instance.LEVEL;
            int lastFeatureLevel = GetLastFeatureLevel();

            if (currentLevel < lastFeatureLevel) return;

            int levelsSince = currentLevel - lastFeatureLevel + 1;
            bool isGiftLevel = levelsSince > 0 && levelsSince % giftInterval == 0;

            bool shouldClaim = isGiftLevel && !HasClaimedGift(currentLevel);
            if (shouldClaim)
                MarkGiftClaimed(currentLevel);

            // Take over the Feature Unlock UI inside win popup
            StartCoroutine(TakeOverWinPopupProgress(currentLevel, lastFeatureLevel, isGiftLevel, shouldClaim));
        }

        private System.Collections.IEnumerator TakeOverWinPopupProgress(
            int currentLevel, int lastFeatureLevel, bool isGiftLevel, bool shouldClaim)
        {
            // Wait for win popup to be instantiated
            yield return null;
            yield return null;

            var popup = UIPopupController.Instance.GetPopup<UIGameWinPopup>();
            if (popup == null) yield break;

            var featureProgress = popup.GetComponentInChildren<UIFeatureProgress>(true);
            if (featureProgress == null) yield break;

            // Disable UIFeatureProgress so it stops hiding the root
            featureProgress.enabled = false;
            featureProgress.gameObject.SetActive(true);

            var rootTf = featureProgress.transform.Find("root");
            if (rootTf != null)
                rootTf.gameObject.SetActive(true);

            var searchRoot = rootTf != null ? rootTf : featureProgress.transform;
            var iconImage = FindChild<Image>(searchRoot, "icon");
            var fillImage = FindChild<Image>(searchRoot, "fill");
            var percentText = FindChild<TMP_Text>(searchRoot, "txt percent");
            var questionGO = FindChildGO(searchRoot, "?");

            int progressInCycle = (currentLevel - lastFeatureLevel + 1) % giftInterval;
            float fill01 = isGiftLevel ? 1f : (float)progressInCycle / giftInterval;
            float previousFill01 = Mathf.Max(0f, fill01 - 1f / giftInterval);

            if (iconImage != null)
            {
                iconImage.sprite = isGiftLevel ? chestUnlockedSprite : chestLockedSprite;
                iconImage.enabled = true;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = 1f - previousFill01;
                fillImage.sprite = isGiftLevel ? chestUnlockedSprite : chestLockedSprite;
            }

            if (percentText != null)
                percentText.text = $"{Mathf.RoundToInt(previousFill01 * 100f)}%";

            // Wait for popup show animation before animating fill
            yield return new WaitForSecondsRealtime(0.8f);

            if (fillImage != null)
                fillImage.DOFillAmount(1f - fill01, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);

            if (percentText != null)
            {
                int fromPct = Mathf.RoundToInt(previousFill01 * 100f);
                int toPct = Mathf.RoundToInt(fill01 * 100f);
                DOTween.To(() => fromPct, x => { fromPct = x; percentText.text = $"{x}%"; }, toPct, 0.6f)
                    .SetEase(Ease.OutCubic).SetUpdate(true);
            }

            if (questionGO != null)
                questionGO.SetActive(!isGiftLevel);

            var featureText = FindChild<TMP_Text>(searchRoot, "txt feature");
            if (featureText != null)
            {
                featureText.gameObject.SetActive(true);
                featureText.text = isGiftLevel
                    ? $"+{giftCoinAmount} coins!"
                    : "Mystery gift!";
            }

            // Coin fly effect on gift levels
            if (shouldClaim)
            {
                // Wait for popup show animation to complete
                yield return new WaitForSecondsRealtime(1.5f);

                var coinHeader = popup.GetComponentInChildren<UICoinHeader>(true);
                if (coinHeader != null && iconImage != null)
                {
                    var iconRect = iconImage.GetComponent<RectTransform>();
                    var headerRect = coinHeader.transform as RectTransform;
                    if (iconRect != null && headerRect != null)
                    {
                        // Convert chest icon position to coin header local space
                        Vector3 worldPos = iconRect.TransformPoint(iconRect.rect.center);
                        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            headerRect, screenPos, null, out var localPos);

                        coinHeader.CollectCoinsFromUI(localPos, giftCoinAmount);
                    }
                    else
                    {
                        UserProfileController.Instance.AddCoin(giftCoinAmount);
                    }
                }
                else
                {
                    UserProfileController.Instance.AddCoin(giftCoinAmount);
                }
            }
        }

        private int GetLastFeatureLevel()
        {
            if (_featureSO == null || _featureSO.config == null || _featureSO.config.Count == 0)
                return 0;

            int max = 0;
            foreach (var c in _featureSO.config)
            {
                if (c != null && c.unlockAtLevel > max)
                    max = c.unlockAtLevel;
            }
            return max;
        }

        private bool HasClaimedGift(int level)
        {
            return PlayerPrefs.GetInt(LastGiftClaimedKey, 0) >= level;
        }

        private void MarkGiftClaimed(int level)
        {
            PlayerPrefs.SetInt(LastGiftClaimedKey, level);
            PlayerPrefs.Save();
        }

        private static T FindChild<T>(Transform parent, string name) where T : Component
        {
            var tf = FindChildTransform(parent, name);
            return tf != null ? tf.GetComponent<T>() : null;
        }

        private static GameObject FindChildGO(Transform parent, string name)
        {
            var tf = FindChildTransform(parent, name);
            return tf != null ? tf.gameObject : null;
        }

        private static Transform FindChildTransform(Transform parent, string name)
        {
            var direct = parent.Find(name);
            if (direct != null) return direct;

            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindChildTransform(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
