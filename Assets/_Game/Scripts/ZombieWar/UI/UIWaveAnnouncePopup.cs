using DG.Tweening;
using Nexzap.Base;
using Nexzap.Base.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Level;

namespace ZombieWar.UI
{
    /// <summary>
    /// Wave / boss announce toast. Follows template flow: SetData → Show → Hide.
    /// Extends UIBasePopup for shared DOTween open/close, but stays non-blocking.
    /// </summary>
    public sealed class UIWaveAnnouncePopup : UIBasePopup
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;

        [SerializeField] private GameObject normalVisualRoot;
        [SerializeField] private GameObject bossVisualRoot;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Color normalBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        [SerializeField] private Color bossBackgroundColor = new Color(0.45f, 0.05f, 0.05f, 0.94f);
        [SerializeField] private Color normalTitleColor = Color.white;
        [SerializeField] private Color bossTitleColor = new Color(1f, 0.25f, 0.25f, 1f);

        public void SetData(in WaveAnnounceInfo info)
        {
            if (titleText != null)
            {
                titleText.text = info.ResolveTitle();
                titleText.color = info.IsBoss ? bossTitleColor : normalTitleColor;
            }

            if (subtitleText != null)
            {
                subtitleText.text = info.ResolveSubtitle();
            }

            if (normalVisualRoot != null)
            {
                normalVisualRoot.SetActive(!info.IsBoss);
            }

            if (bossVisualRoot != null)
            {
                bossVisualRoot.SetActive(info.IsBoss);
            }

            if (panelBackground != null)
            {
                panelBackground.color = info.IsBoss ? bossBackgroundColor : normalBackgroundColor;
            }
        }

        public override void Show()
        {
            playOpenSound();

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panel != null)
            {
                panel.DOKill();
                panel.localScale = Vector3.one * 0.2f;
            }

            gameObject.SetActive(true);

            Sequence seq = DOTween.Sequence();
            if (panel != null)
            {
                seq.Join(panel.DOScale(Vector3.one, ANIM_DURATION).SetEase(Ease.OutBack));
            }

            if (canvasGroup != null)
            {
                seq.Join(canvasGroup.DOFade(1f, ANIM_DURATION));
            }

            seq.SetUpdate(true);
            seq.OnComplete(ShowCompleted);
        }

        public override void Hide()
        {
            playCloseSound();

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panel != null)
            {
                panel.DOKill();
                panel.localScale = Vector3.one;
            }

            gameObject.SetActive(false);
            HideCompleted();
        }

        private static void playOpenSound()
        {
            if (AudioController.Instance == null)
            {
                return;
            }

            AudioController.Instance.PlaySound(SoundName.UI_Warning);
        }

        private static void playCloseSound()
        {
            if (AudioController.Instance == null)
            {
                return;
            }

            AudioController.Instance.PlaySound(SoundName.UI_PopupClose);
        }
    }
}
