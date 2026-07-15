using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nexzap.Base.UI
{
    public abstract class UIBasePopup : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected RectTransform panel;
        public List<UIBaseButton> closeBtns;
        public const float ANIM_DURATION = 0.3f;

        public UnityEvent onClosed = new UnityEvent();
        public UnityEvent onShowed = new UnityEvent();
        private bool isHiding = false;
        protected virtual void Awake()
        {}

        protected virtual void OnDestroy()
        {
            closeBtns.ForEach(c => c?.onClick.RemoveAllListeners());
        }

        public virtual void Show()
        {
            AudioController.Instance.PlaySound(SoundName.UI_PopupOpen);
            
            canvasGroup.alpha = 0;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            panel.transform.localScale = Vector3.one * 0.2f;
            gameObject.SetActive(true);
            Sequence seq = DOTween.Sequence();
            seq.Join(panel.DOScale(Vector3.one, ANIM_DURATION).SetEase(Ease.OutBack));
            seq.Join(canvasGroup.DOFade(1, ANIM_DURATION));
            seq.SetUpdate(true);
            seq.OnComplete(ShowCompleted);
        }

        protected virtual void ShowCompleted()
        {
            onShowed?.Invoke();
        }

        public virtual void Hide()
        {
            AudioController.Instance.PlaySound(SoundName.UI_PopupClose);

            // if(isHiding) return;
            // isHiding = true;
            // Sequence seq = DOTween.Sequence();
            // seq.Join(panel.DOScale(Vector3.one * 0.2f, 0.35f).SetEase(Ease.InBack));
            // // seq.Join(canvasGroup.DOFade(0, 0.15f).SetDelay(0.2f));
            // seq.OnComplete(() => {
            //     canvasGroup.alpha = 0;
            //     gameObject.SetActive(false);
            //     panel.transform.localScale = Vector3.one;
            //     HideCompleted();
            // });

            gameObject.SetActive(false);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            HideCompleted();
        }

        protected virtual void HideCompleted()
        {
            onClosed?.Invoke();
            isHiding = false;
        }
    }
}

