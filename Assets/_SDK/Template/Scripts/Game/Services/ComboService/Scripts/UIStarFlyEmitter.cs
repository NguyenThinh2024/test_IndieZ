using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Thinh.Base.Gameplay
{
    public sealed class UIStarFlyEmitter : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private RectTransform targetRoot;

        [Header("Star")]
        [SerializeField] private Image starIconPrefab;
        [SerializeField] private Image starIconTarget;

        [SerializeField] private float flyDuration = 1.2f;
        [SerializeField] private float spreadRadius = 40f;

        // punch params (giữ như bạn đã làm)
        [Header("Target Punch")]
        [SerializeField] private float punchScale = 0.18f;
        [SerializeField] private float punchDuration = 0.18f;
        [SerializeField] private int punchVibrato = 8;
        [SerializeField] private float punchElasticity = 0.9f;

        private Tween _targetPunchTween;

        public void Emit(int count, Vector3 worldFrom) => Emit(count, worldFrom, null);

        public void Emit(int count, Vector3 worldFrom, Action onArrived)
        {
            if (targetRoot == null || starIconPrefab == null || starIconTarget == null) return;

            Vector2 from = WorldToCanvas(targetRoot, worldFrom);
            Vector2 to = TargetToLocalPoint(targetRoot, starIconTarget.rectTransform);

            int arrived = 0;
            int total = Mathf.Max(1, count);

            for (int i = 0; i < total; i++)
            {
                var img = Instantiate(starIconPrefab, targetRoot);
                img.gameObject.SetActive(true);

                var rt = (RectTransform)img.transform;

                Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
                rt.anchoredPosition = from + offset;
                rt.localScale = Vector3.one;

                Sequence seq = DOTween.Sequence();
                seq.Append(rt.DOAnchorPos(to, flyDuration).SetEase(Ease.OutQuad));
                seq.Join(rt.DOScale(0.6f, flyDuration));

                seq.OnComplete(() =>
                {
                    Destroy(img.gameObject);
                    PunchTargetOnce();

                    arrived++;
                    if (arrived >= total)
                        onArrived?.Invoke();

                    AudioController.Instance.PlaySound(SoundName.UI_CollectGem);
                });
            }
        }

        private void PunchTargetOnce()
        {
            if (starIconTarget == null) return;

            if (_targetPunchTween != null && _targetPunchTween.IsActive())
                _targetPunchTween.Kill(false);

            var t = starIconTarget.rectTransform;
            t.localScale = Vector3.one;

            _targetPunchTween = t.DOPunchScale(
                new Vector3(punchScale, punchScale, 0f),
                punchDuration,
                punchVibrato,
                punchElasticity
            );
        }

        private static Vector2 WorldToCanvas(RectTransform canvas, Vector3 worldPos)
        {
            var cam = Camera.main;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas, screen, null, out Vector2 localPoint);

            return localPoint;
        }

        private static Vector2 TargetToLocalPoint(RectTransform root, RectTransform target)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, target.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root, screen, null, out Vector2 localPoint);

            return localPoint;
        }
    }
}