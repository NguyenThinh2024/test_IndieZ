using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexzap.Base.UI
{
    [RequireComponent(typeof(Button))]
    public class UIBaseButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        // =========================
        // EVENTS
        // =========================
        [HideInInspector] public UnityEvent onClick = new UnityEvent();
        [HideInInspector] public UnityEvent onPointerEnter = new UnityEvent();
        [HideInInspector] public UnityEvent onPointerExit = new UnityEvent();

        // =========================
        // COMPONENTS
        // =========================
        [HideInInspector] public Button button;

        // =========================
        // ANIMATION
        // =========================
        private float animationScale = 1.1f;
        private float animationDuration = 0.1f;

        private bool isPressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClick);
        }

        // =========================
        // CLICK
        // =========================
        private void OnButtonClick()
        {
            if (isPressed)
            {
                return;
            }

            isPressed = true;

            VibrationController.Instance?.VibratePop();
            AudioController.Instance?.PlaySound(SoundName.UI_ButtonClick);

            AnimateButton();
        }

        private void OnDisable()
        {
            ResetState();
        }

        private void AnimateButton()
        {
            transform.DOKill();
            transform.DOScale(Vector3.one * animationScale, animationDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    onClick?.Invoke();

                    transform.DOScale(Vector3.one, animationDuration)
                        .SetEase(Ease.InOutSine)
                        .SetUpdate(true)
                        .OnComplete(() => isPressed = false);
                });
        }

        // =========================
        // POINTER EVENTS
        // =========================
        public void OnPointerEnter(PointerEventData eventData)
        {
            //Debug.Log("OnPointerEnter");
            onPointerEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //Debug.Log("OnPointerExit");
            onPointerExit?.Invoke();
        }

        // =========================
        // BUTTON
        // =========================
        public void SetInteractable(bool interactable)
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.interactable = interactable;
        }

        public void ResetState()
        {
            isPressed = false;
            transform.DOKill();
            transform.localScale = Vector3.one;

            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnDestroy()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
