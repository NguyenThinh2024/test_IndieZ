using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using DG.Tweening;

namespace Thinh.Base.IAP
{
    public class IAPLimitedUI : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text countdownText;
        public Image giftBoxImg, coverImg, lightImg;
        public ParticleSystem lightPs;

        [Header("Config")]
        public float activeDays = 6f;   // hiển thị trong 6 ngày
        public float hideDays = 1f;     // ẩn 1 ngày

        [Header("Animation")]
        [SerializeField] private float coverShakeAngle = 8f;
        [SerializeField] private float coverShakeDuration = 0.4f;
        [SerializeField] private float jumpHeight = 30f;
        [SerializeField] private float jumpUpDuration = 0.45f;
        [SerializeField] private float jumpDownDuration = 0.35f;
        [SerializeField] private float loopDelay = 0.6f;
        [SerializeField] private float squashScaleX = 1.2f;
        [SerializeField] private float squashScaleY = 0.8f;
        [SerializeField] private float squashDuration = 0.15f;
        [SerializeField] private float landingScaleX = 1.12f;
        [SerializeField] private float landingScaleY = 0.88f;
        [SerializeField] private float landingScaleDuration = 0.12f;
        [SerializeField] private float delayTiltAngle = 5f;
        [SerializeField] private float delayTiltDuration = 0.25f;
        [SerializeField] private float postTiltPause = 0.1f;
        [SerializeField] private float coverDropDelay = 0.1f;
        [SerializeField] private float lightMaxAlpha = 1f;
        [SerializeField] private float lightFadeInDuration = 0.2f;
        [SerializeField] private float lightFadeOutDuration = 0.2f;

        private const string KEY_LIMITED_START = "limited_offer_start_time";
        private const string KEY_LIMITED_STATE = "limited_offer_state";

        private enum OfferState { Active, Hidden }
        private OfferState state;

        private DateTime startTime;
        private double activeHours;
        private double hideHours;
        private Sequence animSequence;
        private RectTransform boxRect;
        private RectTransform coverRect;
        private Vector2 boxStartPos;
        private Vector2 coverStartPos;
        private Vector3 coverStartRot;
        private Vector3 boxStartRot;
        private Vector3 boxStartScale;
        private Vector3 coverStartScale;
        private Color lightStartColor;

        private void Awake()
        {
            activeHours = activeDays * 24.0;
            hideHours = hideDays * 24.0;

            LoadState();
            EvaluateState();  // chạy 1 lần khi mở UI

            CacheAnimationRefs();
        }

        private void Update()
        {
            if (state != OfferState.Active) return;

            TimeSpan elapsed = DateTime.UtcNow - startTime;

            // Cập nhật countdown
            TimeSpan remaining = TimeSpan.FromHours(activeHours) - elapsed;
            countdownText.text = FormatTime(remaining);

            // Nếu hết giờ active → tắt ngay
            if (elapsed.TotalHours >= activeHours)
            {
                ChangeToHidden();
            }
        }

        private void OnEnable()
        {
            PlayIdleAnimation();
        }

        private void OnDisable()
        {
            KillAnimation();
        }

        // ======================================================
        // LOAD STATE
        // ======================================================
        private void LoadState()
        {
            if (!PlayerPrefs.HasKey(KEY_LIMITED_START))
            {
                PlayerPrefs.SetString(KEY_LIMITED_START, DateTime.UtcNow.ToString());
                PlayerPrefs.SetInt(KEY_LIMITED_STATE, (int)OfferState.Active);
            }

            startTime = DateTime.Parse(PlayerPrefs.GetString(KEY_LIMITED_START));
            state = (OfferState)PlayerPrefs.GetInt(KEY_LIMITED_STATE);
        }

        // ======================================================
        // INITIAL CHECK IN AWAKE
        // ======================================================
        private void EvaluateState()
        {
            TimeSpan elapsed = DateTime.UtcNow - startTime;

            if (state == OfferState.Active)
            {
                // Nếu hết Active time thì chuyển sang Hidden
                if (elapsed.TotalHours >= activeHours)
                {
                    ChangeToHidden();
                    return;
                }

                // Cập nhật countdown ngay lúc Awake
                TimeSpan remaining = TimeSpan.FromHours(activeHours) - elapsed;
                countdownText.text = FormatTime(remaining);

                gameObject.SetActive(true);
            }
            else
            {
                // Đang Hidden → kiểm tra xem đã hết thời gian ẩn chưa
                if (elapsed.TotalHours >= activeHours + hideHours)
                {
                    ResetOffer();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        // ======================================================
        // STATE CHANGE HANDLERS
        // ======================================================
        private void ChangeToHidden()
        {
            state = OfferState.Hidden;
            PlayerPrefs.SetInt(KEY_LIMITED_STATE, (int)OfferState.Hidden);

            gameObject.SetActive(false);
        }

        private void ResetOffer()
        {
            state = OfferState.Active;

            startTime = DateTime.UtcNow;
            PlayerPrefs.SetString(KEY_LIMITED_START, startTime.ToString());
            PlayerPrefs.SetInt(KEY_LIMITED_STATE, (int)OfferState.Active);

            gameObject.SetActive(true);
        }

        // ======================================================
        // FORMAT TIME
        // ======================================================
        private string FormatTime(TimeSpan t)
        {
            int days = t.Days;
            int hours = t.Hours;
            int minutes = t.Minutes;

            // ≥ 1 day → show: Xd Yh Zm
            if (days >= 1)
                return $"{days}d {hours}h {minutes}m";

            // ≥ 1 hour → show: Yh Zm
            if (hours >= 1)
                return $"{hours}h {minutes}m";

            // only minutes → Zm
            return $"{minutes}m";
        }

        // ======================================================
        // ANIMATION (DOTween)
        // ======================================================
        private void CacheAnimationRefs()
        {
            boxRect = giftBoxImg != null ? giftBoxImg.rectTransform : null;
            coverRect = coverImg != null ? coverImg.rectTransform : null;

            if (boxRect != null)
                boxStartPos = boxRect.anchoredPosition;
            if (boxRect != null)
            {
                boxStartScale = boxRect.localScale;
                boxStartRot = boxRect.localEulerAngles;
            }

            if (coverRect != null)
            {
                coverStartPos = coverRect.anchoredPosition;
                coverStartRot = coverRect.localEulerAngles;
                coverStartScale = coverRect.localScale;
            }

            if (lightImg != null)
            {
                lightStartColor = lightImg.color;
                var c = lightImg.color;
                c.a = 0f;
                lightImg.color = c;
            }
        }

        private void PlayIdleAnimation()
        {
            KillAnimation();

            if (boxRect == null || coverRect == null) return;

            animSequence = DOTween.Sequence();

            AppendRotateSegment();

            AppendSquashSegment();

            // Nhảy lên cùng lúc
            Vector2 boxUpPos = boxStartPos + new Vector2(0f, jumpHeight);
            Vector2 coverUpPos = coverStartPos + new Vector2(0f, jumpHeight * 1.6f);

            animSequence.Append(boxRect.DOAnchorPos(boxUpPos, jumpUpDuration).SetEase(Ease.OutQuad));
            animSequence.Join(coverRect.DOAnchorPos(coverUpPos, jumpUpDuration).SetEase(Ease.OutQuad));

            animSequence.Join(boxRect.DOScale(boxStartScale, jumpUpDuration * 0.8f).SetEase(Ease.OutQuad));
            animSequence.Join(coverRect.DOScale(coverStartScale, jumpUpDuration * 0.8f).SetEase(Ease.OutQuad));
            animSequence.Join(
                coverRect.DOLocalRotate(
                    new Vector3(coverStartRot.x, coverStartRot.y, coverStartRot.z + coverShakeAngle),
                    coverShakeDuration
                ).SetLoops(6, LoopType.Yoyo).SetEase(Ease.InOutSine)
            );
            if (lightImg != null)
            {
                animSequence.Join(
                    lightImg.DOFade(lightMaxAlpha, lightFadeInDuration).SetDelay(jumpUpDuration * 0.5f)
                );
            }
            if (lightPs != null) {
                animSequence.Join(DOTween.Sequence()
                    .AppendCallback(() => lightPs.Play(true))
                    .AppendInterval(jumpUpDuration * 0.5f)
                );
            }
    
            // Rơi xuống lại vị trí cũ
            animSequence.Append(boxRect.DOAnchorPos(boxStartPos, jumpDownDuration).SetEase(Ease.InQuad));
            if (lightImg != null)
            {
                animSequence.Join(
                    lightImg.DOFade(0f, lightFadeOutDuration)
                );
            }

            animSequence.Join(
                coverRect.DOAnchorPos(coverUpPos + new Vector2(0f, jumpHeight) , jumpDownDuration)
                    .SetEase(Ease.InQuad)
            );
            animSequence.Append(
                coverRect.DOAnchorPos(coverStartPos, jumpDownDuration * 1f)
                    .SetEase(Ease.InQuad)
            );
        
            // Nắp dập xuống rồi bật nhẹ về scale gốc
            Vector3 coverLandingScale = new Vector3(coverStartScale.x * landingScaleX, coverStartScale.y * landingScaleY, coverStartScale.z);
            Vector3 boxLandingScale = new Vector3(boxStartScale.x * landingScaleX, boxStartScale.y * landingScaleY, boxStartScale.z);

            animSequence.Append(coverRect.DOScale(coverLandingScale, landingScaleDuration).SetEase(Ease.OutQuad));
            animSequence.Join(boxRect.DOScale(boxLandingScale, landingScaleDuration).SetEase(Ease.OutQuad));

            animSequence.Append(coverRect.DOScale(coverStartScale, landingScaleDuration * 0.8f).SetEase(Ease.OutQuad));
            animSequence.Join(boxRect.DOScale(boxStartScale, landingScaleDuration * 0.8f).SetEase(Ease.OutQuad));

            animSequence.SetLoops(-1, LoopType.Restart);
        }

        private void AppendRotateSegment()
        {
            if (animSequence == null) return;

            for (int i = 0; i < 3; i++)
            {
                animSequence.Append(
                    boxRect.DOLocalRotate(
                        new Vector3(boxStartRot.x, boxStartRot.y, boxStartRot.z + delayTiltAngle),
                        delayTiltDuration
                    ).SetEase(Ease.InOutSine)
                );
                animSequence.Join(
                    coverRect.DOLocalRotate(
                        new Vector3(coverStartRot.x, coverStartRot.y, coverStartRot.z + delayTiltAngle),
                        delayTiltDuration
                    ).SetEase(Ease.InOutSine)
                );

                animSequence.Append(
                    boxRect.DOLocalRotate(
                        new Vector3(boxStartRot.x, boxStartRot.y, boxStartRot.z - delayTiltAngle),
                        delayTiltDuration
                    ).SetEase(Ease.InOutSine)
                );
                animSequence.Join(
                    coverRect.DOLocalRotate(
                        new Vector3(coverStartRot.x, coverStartRot.y, coverStartRot.z - delayTiltAngle),
                        delayTiltDuration
                    ).SetEase(Ease.InOutSine)
                );
            }

            animSequence.Append(
                boxRect.DOLocalRotate(boxStartRot, delayTiltDuration * 0.6f).SetEase(Ease.InOutSine)
            );
            animSequence.Join(
                coverRect.DOLocalRotate(coverStartRot, delayTiltDuration * 0.6f).SetEase(Ease.InOutSine)
            );

            if (postTiltPause > 0f)
                animSequence.AppendInterval(postTiltPause);
        }

        private void AppendSquashSegment()
        {
            if (animSequence == null) return;

            Vector3 boxSquash = new Vector3(boxStartScale.x * squashScaleX * 1.2f, boxStartScale.y * squashScaleY, boxStartScale.z);
            Vector3 coverSquash = new Vector3(coverStartScale.x * squashScaleX * 0.8f, coverStartScale.y * squashScaleY, coverStartScale.z);

            animSequence.Append(boxRect.DOScale(boxSquash, squashDuration).SetEase(Ease.OutQuad));
            animSequence.Join(coverRect.DOScale(coverSquash, squashDuration).SetEase(Ease.OutQuad));
        }

        private void KillAnimation()
        {
            if (animSequence != null && animSequence.IsActive())
            {
                animSequence.Kill();
            }

            // Reset về trạng thái ban đầu
            if (boxRect != null)
            {
                boxRect.anchoredPosition = boxStartPos;
                boxRect.localScale = boxStartScale;
                boxRect.localEulerAngles = boxStartRot;
            }

            if (coverRect != null)
            {
                coverRect.anchoredPosition = coverStartPos;
                coverRect.localEulerAngles = coverStartRot;
                coverRect.localScale = coverStartScale;
            }
        }
    }
}
