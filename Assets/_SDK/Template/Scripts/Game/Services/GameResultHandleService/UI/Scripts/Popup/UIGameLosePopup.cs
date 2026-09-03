using System;
using Thinh.Base.Ads;
using Thinh.Base.Data;
using Thinh.Base.Level;
using Thinh.Base.UI;
using Thinh.Base.UI.Template;
using Thinh.Template;
using TMPro;
using UnityEngine;

namespace Thinh.Base
{
    public class UIGameLosePopup : UIBasePopup
    {
        [SerializeField] private TextMeshProUGUI levelTxt;
        [SerializeField] private UIBaseButton retryBtn;

        private LosePopupData _data;
        public Action OnRetry;

        private void OnEnable()
        {
            retryBtn?.onClick?.AddListener(OnClickRetry);
        }

        private void OnDisable()
        {
            retryBtn?.onClick?.RemoveListener(OnClickRetry);
        }

        public void SetData(LosePopupData data)
        {
            _data = data;
            levelTxt.text = $"Level {data.level}";
        }

        protected override void HideCompleted()
        {
            base.HideCompleted();
            OnRetry?.Invoke();
            OnRetry = null;
        }

        private void OnClickRetry()
        {
            UIPopupController.Instance.HidePopup<UIGameLosePopup>();
        }
    }
}
