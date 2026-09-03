using System;
using System.Collections;
using System.Collections.Generic;
using Thinh.Base;
using Thinh.Base.Ads;
using Thinh.Base.Data;
using Thinh.Base.UI;
using TMPro;
using UnityEngine;
using Thinh.Base.Gameplay;
using Thinh.Base.UI.Template;
using UnityEngine.Events;
using DG.Tweening;

namespace Thinh.Template
{
    public class UIGameWinPopup : UIBasePopup
    {
        [SerializeField] private TextMeshProUGUI levelTxt;
        [SerializeField] private TextMeshProUGUI coinTxt;
        [SerializeField] private UIBaseButton continueBtn;

        [Header("Coin Collect FX")]
        [Tooltip("Spawner coin bay lên header (đã set headerCoinTarget trong spawner).")]
        [SerializeField] private UICoinHeader coinHeader;

        //[Tooltip("Điểm coin spawn trong popup (RectTransform). Nếu null -> dùng coinTxt.rectTransform.")]
        //[SerializeField] private RectTransform coinFlyStart;

        [Tooltip("Chờ thêm chút sau khi coin bay xong rồi mới hiện nút Continue.")]
        [SerializeField] private float afterCollectDelay = 0.1f;

        private int coinReward;
        private LevelService _levelService;
        private Coroutine collectRoutine;
        private bool isContinuing;
        public Action OnNext;

        protected override void Awake()
        {
            base.Awake();
            if (GameController.Instance != null)
            {
                GameController.Instance.Services.TryGet(out _levelService);
            }
        }

        protected override void OnDestroy()
        {
            StopCollectRoutine();
            base.OnDestroy();
        }

        private void OnEnable()
        {
            continueBtn.onClick.AddListener(OnClickContinue);
        }

        private void OnDisable()
        {
            continueBtn?.onClick?.RemoveListener(OnClickContinue);
        }


        public override void Show()
        {
            //base.Show();

            ResetContinueState();
            AudioController.Instance.PlaySound(SoundName.UI_LevelComplete);

            canvasGroup.DOKill();
            panel.DOKill();
            canvasGroup.alpha = 0;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            panel.transform.localScale = Vector3.zero;
            gameObject.SetActive(true);
            Sequence seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(1, ANIM_DURATION));
            seq.AppendInterval(0.5f);
            seq.AppendCallback(() =>
            {
                AudioController.Instance.PlaySound(SoundName.UI_Win);
                panel.transform.localScale = Vector3.one * 0.25f;
            });
            seq.Append(panel.DOScale(Vector3.one, ANIM_DURATION).SetEase(Ease.OutBack));   
            seq.SetUpdate(true);
            seq.OnComplete(ShowCompleted);

            int level1Based = _levelService != null
                ? _levelService.CurrentLevelNumber
                : (UserProfileController.Instance != null ? UserProfileController.Instance.LEVEL : 1);

            if (levelTxt != null)
                levelTxt.text = $"Level {level1Based}";

            coinReward = ConfigController.Instance.GameConfig.levelReward;
            if (coinTxt != null)
                coinTxt.text = $"+ {coinReward}";

            if (continueBtn != null)
            {
                continueBtn.ResetState();
                continueBtn.gameObject.SetActive(true);
                continueBtn.SetInteractable(true);
            }
        }

        private IEnumerator CoCollectThenShowButton(UnityAction callback)
        {
            if (coinHeader != null && coinReward > 0)
            {
                RectTransform start = continueBtn.GetComponent<RectTransform>();

                if (start != null)
                {
                    Vector2 startInTarget = ConvertRectToTargetLocal(start, coinHeader);
                    coinHeader.CollectCoinsFromUI(startInTarget, coinReward);
                }
                else
                {
                    UserProfileController.Instance.AddCoin(coinReward);
                }

                // Wait for the estimated coin fly duration.
                yield return new WaitForSecondsRealtime(2f);
            }
            else
            {
                if (coinReward > 0)
                {
                    UserProfileController.Instance.AddCoin(coinReward);
                }
            }

            if (afterCollectDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(afterCollectDelay);
            }

            callback?.Invoke();
        }

        private void OnClickContinue()
        {
            if (isContinuing)
            {
                return;
            }

            isContinuing = true;
            AudioController.Instance.PlaySound(SoundName.UI_ClaimReward);

            if (continueBtn != null)
            {
                continueBtn.SetInteractable(false);
            }

            StopCollectRoutine();
            collectRoutine = StartCoroutine(CoCollectThenShowButton(() =>
            {
                collectRoutine = null;
                if (UITutorialPopupController.TryGetInstance(out UITutorialPopupController tutorialPopupController))
                {
                    tutorialPopupController.HidePopup<UIGameWinPopup>();
                }
                else if (UIPopupController.Instance != null)
                {
                    UIPopupController.Instance.HidePopup<UIGameWinPopup>();
                }
                else
                {
                    Hide();
                }

                OnNext?.Invoke();
            }));
        }

        public override void Hide()
        {
            ResetContinueState();
            base.Hide();
        }

        private void ResetContinueState()
        {
            StopCollectRoutine();
            isContinuing = false;
            if (continueBtn == null)
            {
                return;
            }

            continueBtn.gameObject.SetActive(true);
            continueBtn.ResetState();
            continueBtn.SetInteractable(true);
            continueBtn.transform.localScale = Vector3.one;
        }

        private void StopCollectRoutine()
        {
            if (collectRoutine == null)
            {
                return;
            }

            StopCoroutine(collectRoutine);
            collectRoutine = null;
        }

        /// <summary>
        /// Converts the start RectTransform position to the local space expected by CollectCoinsFromUI.
        /// </summary>
        private Vector2 ConvertRectToTargetLocal(RectTransform startRect, UICoinHeader header)
        {
            RectTransform targetRect = header.transform as RectTransform;
            if (targetRect == null)
            {
                return Vector2.zero;
            }

            Vector3 world = startRect.TransformPoint(startRect.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screen, null, out var local);
            return local;
        }
    }
}
