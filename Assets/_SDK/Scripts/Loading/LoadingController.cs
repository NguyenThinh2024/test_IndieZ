using System.Collections;
using Cysharp.Threading.Tasks;
using Nexzap.Base.UI;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Infrastructure;

namespace Nexzap.Base
{
    /// <summary>
    /// Boot flow: Loading (warmup Addressables) → Menu.
    /// Play on Menu enters Gameplay at UserProfile LEVEL.
    /// </summary>
    public class LoadingController : MonoBehaviour
    {
        [SerializeField] private Image progressBar;
        [SerializeField] private float minVisibleSeconds = 0.35f;
        [SerializeField] private bool warmupAddressables = true;

        private GameManager gameManager;
        private bool hasStartedFlow;

        private void Start()
        {
            gameManager = GameManager.Instance;
            gameManager.OnInited.AddListener(OnInited);

            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }

            if (gameManager.IsInited)
            {
                OnInited();
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnInited.RemoveListener(OnInited);
            }
        }

        private void OnInited()
        {
            if (hasStartedFlow)
            {
                return;
            }

            hasStartedFlow = true;
            StartCoroutine(WarmupAndEnterMenu());
        }

        private IEnumerator WarmupAndEnterMenu()
        {
            float startTime = Time.realtimeSinceStartup;

            if (warmupAddressables)
            {
                UniTask warmupTask = ZombieWarAddressableWarmup.WarmupAsync(
                    new ProgressReporter(p =>
                    {
                        if (progressBar != null)
                        {
                            progressBar.fillAmount = Mathf.Clamp01(p);
                        }
                    }),
                    this.GetCancellationTokenOnDestroy());

                yield return warmupTask.ToCoroutine();
            }

            if (progressBar != null)
            {
                progressBar.fillAmount = 1f;
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < minVisibleSeconds)
            {
                yield return new WaitForSecondsRealtime(minVisibleSeconds - elapsed);
            }

            yield return null;

            GameDataHelper.PlayType = "loading";
            UISceneController.Instance.ChangeScene(SceneName.Menu);
        }

        private sealed class ProgressReporter : System.IProgress<float>
        {
            private readonly System.Action<float> onReport;

            public ProgressReporter(System.Action<float> onReport)
            {
                this.onReport = onReport;
            }

            public void Report(float value)
            {
                onReport?.Invoke(value);
            }
        }
    }
}
