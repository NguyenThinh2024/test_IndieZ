using Nexzap.Base;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nexzap.Base.UI
{
    public class UIPopupController : MonoSingleton<UIPopupController>
    {
        private const bool DisableAllPopupsInPackOnlyMode = false;
        private static bool _loggedDisableWarning;

        [SerializeField] private UIPopupConfig config;
        private readonly Dictionary<string, UIBasePopup> activePopups = new Dictionary<string, UIBasePopup>();

        public T AddPopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();
            if (!CanUsePopupSystem(dialogType))
            {
                return null;
            }

            if (activePopups.TryGetValue(dialogType, out var existingPopup) && existingPopup != null)
            {
                return existingPopup as T;
            }

            var prefab = config.popups.Find(x => x is T) as T;
            if (prefab == null)
            {
                Debug.LogWarning($"[UIPopupController] Popup prefab for {dialogType} is not configured.");
                return null;
            }

            var newDialog = Instantiate(prefab, transform);
            activePopups[dialogType] = newDialog;

            if (newDialog.closeBtns != null)
            {
                foreach (var button in newDialog.closeBtns)
                {
                    if (button != null)
                    {
                        button.onClick.AddListener(() => HidePopup<T>());
                    }
                }
            }

            return newDialog;
        }

        public T GetPopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();
            if (!CanUsePopupSystem(dialogType))
            {
                return null;
            }

            if (!activePopups.ContainsKey(dialogType))
            {
                AddPopup<T>();
            }

            if (!activePopups.TryGetValue(dialogType, out var dialog))
            {
                return null;
            }

            return dialog as T;
        }

        public T GetActivePopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();
            if (!CanUsePopupSystem(dialogType))
            {
                return null;
            }

            if (!activePopups.ContainsKey(dialogType))
            {
                AddPopup<T>();
            }

            var dialog = GetPopup<T>();
            if (dialog != null)
            {
                dialog.transform.SetAsLastSibling();
            }

            return dialog as T;
        }

        public void HidePopup<T>() where T : UIBasePopup
        {
            if (DisableAllPopupsInPackOnlyMode)
            {
                return;
            }

            var dialogType = typeof(T).ToString();
            if (activePopups.TryGetValue(dialogType, out var dialog) && dialog != null)
            {
                dialog.Hide();
            }
        }

        public bool AnyPopupActive()
        {
            if (DisableAllPopupsInPackOnlyMode)
            {
                return false;
            }

            foreach (var item in activePopups)
            {
                if (item.Value != null && item.Value.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        public bool AnyPopupActiveExcept(UIBasePopup ignoredPopup)
        {
            if (DisableAllPopupsInPackOnlyMode)
            {
                return false;
            }

            foreach (var item in activePopups)
            {
                UIBasePopup popup = item.Value;
                if (popup != null && popup != ignoredPopup && popup.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsActivePopup<T>() where T : UIBasePopup
        {
            if (DisableAllPopupsInPackOnlyMode)
            {
                return false;
            }

            var dialogType = typeof(T).ToString();
            return activePopups.ContainsKey(dialogType)
                && activePopups[dialogType] != null
                && activePopups[dialogType].gameObject.activeInHierarchy;
        }

        public void HideAllPopups()
        {
            if (DisableAllPopupsInPackOnlyMode)
            {
                return;
            }

            foreach (var dialog in activePopups.Values)
            {
                if (dialog != null)
                {
                    dialog.gameObject.SetActive(false);
                }
            }
        }

        private static bool CanUsePopupSystem(string dialogType)
        {
            if (!DisableAllPopupsInPackOnlyMode)
            {
                return true;
            }

            if (!_loggedDisableWarning)
            {
                Debug.LogWarning("[UIPopupController] All popups are temporarily disabled for the pack-only Gameplay setup until the original loading/bootstrap flow is restored.");
                _loggedDisableWarning = true;
            }

            return false;
        }
    }
}
