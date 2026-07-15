using System;
using Nexzap.Base.UI;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Nexzap.Template;
using TMPro;
using Nexzap.Base;

public class UIGameFailPopup : UIBasePopup
{
    [Header("UI")]
    [SerializeField] private Image loseImage;
    [SerializeField] private TextMeshProUGUI loseText;

    private Vector3 startPos;
    private Vector3 centerPos;

    private Action _onFinished;

    protected override void Awake()
    {
        base.Awake();
        centerPos = panel.localPosition;
        startPos = centerPos + new Vector3(0, Screen.height, 0);
    }

    public void SetData(GameFailPopupData data)
    {
        SetLoseSprite(data.loseSprite);
        SetLoseText(data.loseText);
    }

    public void SetLoseSprite(Sprite sprite)
    {
        if (loseImage != null)
        {
            loseImage.sprite = sprite;
            loseImage.enabled = sprite != null;
        }
    }

    public void SetLoseText(string text)
    {
        if (loseText == null) return;

        bool has = !string.IsNullOrEmpty(text);
        loseText.gameObject.SetActive(has);
        if (has) loseText.text = text;
    }

    public void SetOnFinished(Action onFinished)
    {
        _onFinished = onFinished;
    }

    public override void Show()
    {
        AudioController.Instance.PlaySound(SoundName.UI_Lose);

        canvasGroup.alpha = 0;
        gameObject.SetActive(true);

        panel.localPosition = startPos;

        Sequence seq = DOTween.Sequence();
        seq.Join(canvasGroup.DOFade(1, 0.5f));
        seq.Join(panel.DOLocalMove(centerPos, 0.5f).SetEase(Ease.OutBack));
        seq.AppendInterval(1.5f);
        seq.OnComplete(() =>
        {
            UIPopupController.Instance.HidePopup<UIGameFailPopup>();
        });
    }

    public override void Hide()
    {
        Sequence seq = DOTween.Sequence();
        seq.Join(panel.DOLocalMove(startPos, 0.5f).SetEase(Ease.InBack));
        seq.Join(canvasGroup.DOFade(0, 0.5f));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            HideCompleted();
        });
    }

    protected override void HideCompleted()
    {
        base.HideCompleted();

        _onFinished?.Invoke();
        _onFinished = null;
    }
}