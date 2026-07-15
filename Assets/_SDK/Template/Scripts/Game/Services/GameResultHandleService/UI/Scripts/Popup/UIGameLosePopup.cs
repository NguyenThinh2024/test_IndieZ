using System;
using Nexzap.Base.Ads;
using Nexzap.Base.Data;
using Nexzap.Base.Level;
using Nexzap.Base.UI;
using Nexzap.Base.UI.Template;
using Nexzap.Template;
using TMPro;
using UnityEngine;

namespace Nexzap.Base
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
