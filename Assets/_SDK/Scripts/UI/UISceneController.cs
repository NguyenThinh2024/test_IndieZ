using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Thinh.Base.UI
{
    public class SceneName
    {
        public const string Loading = "Loading";
        /// <summary>Main menu scene asset name is Menu.unity.</summary>
        public const string Menu = "Menu";
        /// <summary>Alias kept for template callers (Quit popup, etc.).</summary>
        public const string Home = Menu;
        public const string Gameplay = "Gameplay";
    }

    public class UISceneController : MonoSingleton<UISceneController>
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup root;   // full screen overlay
        [SerializeField] private Image bg;           // optional (chỉ để set sprite/color), không bắt buộc

        [Header("Fade")]
        [SerializeField] private float fadeInDur = 0.25f;   // overlay alpha 0 -> 1
        [SerializeField] private float fadeOutDur = 0.25f;  // overlay alpha 1 -> 0
        [SerializeField] private Ease fadeEase = Ease.Linear;

        [Header("Stability")]
        [SerializeField] private int waitFramesAfterSceneActive = 2;

        public string CurrentScene;

        private Coroutine _co;
        private Tween _fadeTween;

        public override void Init()
        {
            base.Init();
            DontDestroyOnLoad(gameObject);

            if (root != null)
            {
                root.alpha = 0f;
                root.blocksRaycasts = false;
                root.interactable = false;
            }
        }

        public void ReloadCurrentScene()
        {
            if (!string.IsNullOrEmpty(CurrentScene))
                ChangeScene(CurrentScene);
        }

        public void ChangeScene(string sceneName)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(CoLoad(sceneName));
            CurrentScene = sceneName;
            //SceneManager.LoadScene(sceneName);
        }

        private IEnumerator CoLoad(string sceneName)
        {
            KillTween();

            // 1) Fade IN overlay
            if (root != null)
            {
                root.blocksRaycasts = true;
                root.interactable = true;

                root.alpha = 0f;
                _fadeTween = root.DOFade(1f, fadeInDur).SetEase(fadeEase).SetUpdate(true);
                yield return _fadeTween.WaitForCompletion();
            }

            // 2) Load async scene
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f) yield return null;

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            while (SceneManager.GetActiveScene().name != sceneName) yield return null;
            for (int i = 0; i < waitFramesAfterSceneActive; i++) yield return null;
            yield return new WaitForEndOfFrame();

            // 3) Fade OUT overlay
            KillTween();
            if (root != null)
            {
                _fadeTween = root.DOFade(0f, fadeOutDur).SetEase(fadeEase).SetUpdate(true);
                yield return _fadeTween.WaitForCompletion();

                root.blocksRaycasts = false;
                root.interactable = false;
            }

            _co = null;
        }

        private void KillTween()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }

        private void OnDisable()
        {
            KillTween();
            if (_co != null) StopCoroutine(_co);
            _co = null;
        }
    }
}