using System.Collections.Generic;
using Nexzap.Base.UI;
using Nexzap.Template;
using UnityEngine;
using UnityEngine.UI;

public class UITutorialPopupController : MonoBehaviour
{
    private static UITutorialPopupController instance;

    [SerializeField] private UIPopupConfig config;
    [SerializeField] private Transform popupRoot;

    private readonly Dictionary<string, UIBasePopup> activePopups = new Dictionary<string, UIBasePopup>();

    public static bool TryGetInstance(out UITutorialPopupController controller)
    {
        controller = instance;
        return controller != null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"{nameof(UITutorialPopupController)}: multiple instances found, destroying duplicate on '{name}'.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolvePopupRoot();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public T AddPopup<T>() where T : UIBasePopup
    {
        string popupType = typeof(T).ToString();
        if (activePopups.TryGetValue(popupType, out UIBasePopup existingPopup) && existingPopup != null)
        {
            return existingPopup as T;
        }

        T prefab = ResolvePopupPrefab<T>();
        if (prefab == null)
        {
            Debug.LogWarning($"[{nameof(UITutorialPopupController)}] Popup prefab for {popupType} is not configured.", this);
            return null;
        }

        ResolvePopupRoot();
        T popup = Instantiate(prefab, popupRoot != null ? popupRoot : transform);
        activePopups[popupType] = popup;

        if (popup.closeBtns != null)
        {
            foreach (UIBaseButton button in popup.closeBtns)
            {
                if (button != null)
                {
                    button.onClick.AddListener(() => HidePopup<T>());
                }
            }
        }

        return popup;
    }

    public T GetPopup<T>() where T : UIBasePopup
    {
        string popupType = typeof(T).ToString();
        if (!activePopups.ContainsKey(popupType))
        {
            AddPopup<T>();
        }

        return activePopups.TryGetValue(popupType, out UIBasePopup popup) ? popup as T : null;
    }

    public T GetActivePopup<T>() where T : UIBasePopup
    {
        T popup = GetPopup<T>();
        if (popup != null)
        {
            popup.transform.SetAsLastSibling();
        }

        return popup;
    }

    public void HidePopup<T>() where T : UIBasePopup
    {
        string popupType = typeof(T).ToString();
        if (activePopups.TryGetValue(popupType, out UIBasePopup popup) && popup != null)
        {
            popup.Hide();
        }
    }

    public void HideAllPopups()
    {
        foreach (UIBasePopup popup in activePopups.Values)
        {
            if (popup != null)
            {
                popup.Hide();
            }
        }
    }

    private T ResolvePopupPrefab<T>() where T : UIBasePopup
    {
        if (config == null || config.popups == null)
        {
            return null;
        }

        if (typeof(T) != typeof(UIGameWinPopup) && typeof(T) != typeof(UIPopupHole))
        {
            return null;
        }

        return config.popups.Find(popup => popup is T) as T;
    }

    private void OnValidate()
    {
        ResolvePopupRoot();
    }

    private void ResolvePopupRoot()
    {
        if (popupRoot != null)
        {
            return;
        }

        Canvas canvasInParents = GetComponentInParent<Canvas>(true);
        if (canvasInParents != null)
        {
            popupRoot = canvasInParents.transform;
            return;
        }

#if UNITY_2023_1_OR_NEWER
        Canvas anyCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
#else
        Canvas anyCanvas = FindObjectOfType<Canvas>(true);
#endif
        if (anyCanvas != null)
        {
            popupRoot = anyCanvas.transform;
        }
    }
}
