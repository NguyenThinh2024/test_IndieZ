using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    /// <summary>
    /// Full-bleed red canvas flash for boss wave announce.
    /// Non-blocking overlay — owned by WaveAnnouncePresenter, not by WaveManager.
    /// </summary>
    public sealed class UIWaveBossFlashOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image flashImage;
        [SerializeField] private Color flashColor = new Color(0.85f, 0.05f, 0.05f, 0.55f);
        [SerializeField] private float fadeInDuration = 0.12f;
        [SerializeField] private float holdDuration = 0.08f;
        [SerializeField] private float fadeOutDuration = 0.28f;
        [SerializeField] private int pulseCount = 4;

        private Tween activeTween;

        private void Awake()
        {
            ensureReady(hidden: true);
        }

        private void OnDestroy()
        {
            killTween();
        }

        public void Play()
        {
            ensureReady(hidden: false);
            killTween();

            if (flashImage != null)
            {
                flashImage.color = flashColor;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(true);

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            int pulses = Mathf.Max(1, pulseCount);
            for (int i = 0; i < pulses; i++)
            {
                seq.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
                if (holdDuration > 0f)
                {
                    seq.AppendInterval(holdDuration);
                }

                seq.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
            }

            seq.OnComplete(() =>
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                activeTween = null;
            });

            activeTween = seq;
        }

        public void Stop()
        {
            killTween();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
        }

        private void ensureReady(bool hidden)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (flashImage == null)
            {
                flashImage = GetComponent<Image>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            if (hidden)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
            }
        }

        private void killTween()
        {
            if (activeTween == null)
            {
                return;
            }

            activeTween.Kill();
            activeTween = null;
        }
    }
}
