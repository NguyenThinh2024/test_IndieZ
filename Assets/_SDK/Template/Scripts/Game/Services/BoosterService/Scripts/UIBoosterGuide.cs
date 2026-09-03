using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Thinh.Base.Data;
using Thinh.Base.UI;

namespace Thinh.Base.Gameplay
{
    public class UIBoosterGuide : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI titleTxt;
        [SerializeField] private TextMeshProUGUI descTxt;
        [SerializeField] private UIBaseButton closeBtn;

        private BoosterService _service;

        public void Bind(BoosterService service)
        {
            _service = service;

            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveListener(OnClose);
                closeBtn.onClick.AddListener(OnClose);
            }
        }

        public void Show(BoosterConfig cfg)
        {
            if (cfg == null) return;

            if (icon != null) icon.sprite = cfg.icon;

            if (titleTxt != null) titleTxt.text = cfg.boosterName;

            if (descTxt != null) descTxt.text = cfg.guideDsc;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnClose()
        {
            // cancel armed booster
            _service?.CancelArmed();
            Hide();
        }
    }
}