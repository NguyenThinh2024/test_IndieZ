using System.Collections.Generic;
using Thinh.Base.Data;
using UnityEngine;

namespace Thinh.Base.Gameplay
{
    public sealed class BoosterUIBinder : MonoBehaviour
    {
        [SerializeField] private List<UIBoosterButton> buttons = new();
        [SerializeField] private UIBoosterGuide guide;

        private BoosterService service;

        public void Bind(BoosterService boosterService)
        {
            if (service != null)
            {
                service.OnStateChanged -= Refresh;
                service.OnArmedChanged.RemoveListener(RefreshAll);
            }

            service = boosterService;
            if (service == null)
            {
                return;
            }

            service.OnStateChanged += Refresh;
            service.OnArmedChanged.AddListener(RefreshAll);

            for (int i = 0; i < buttons.Count; i++)
            {
                UIBoosterButton button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                button.Bind(service);
                Refresh(button.Type);
            }

            if (guide != null)
            {
                guide.Bind(service);
            }
        }

        public void Refresh(BoosterType type)
        {
            if (service == null)
            {
                return;
            }

            bool hasArmed = service.ArmedBooster.HasValue;
            BoosterType armedType = hasArmed ? service.ArmedBooster.Value : default;

            for (int i = 0; i < buttons.Count; i++)
            {
                UIBoosterButton button = buttons[i];
                if (button == null || button.Type != type)
                {
                    continue;
                }

                bool lockOthers = hasArmed && armedType != button.Type;
                BoosterService.BoosterState state = service.GetState(button.Type);
                button.Render(state, lockOthers);
                return;
            }
        }

        private void RefreshAll(BoosterType? _)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (service == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                UIBoosterButton button = buttons[i];
                if (button != null)
                {
                    Refresh(button.Type);
                }
            }
        }

        private void OnDestroy()
        {
            if (service == null)
            {
                return;
            }

            service.OnStateChanged -= Refresh;
            service.OnArmedChanged.RemoveListener(RefreshAll);
        }
    }
}
