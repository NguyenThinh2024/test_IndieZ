#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Level;
using ZombieWar.UI;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Creates wave/boss announce UI under Gameplay canvas and wires WaveAnnouncePresenter.
    /// Menu: Zombie War / Setup Wave Announce UI
    /// </summary>
    public static class ZombieWarWaveAnnounceSceneSetup
    {
        private const string MenuPath = "Zombie War/Setup Wave Announce UI";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            WaveManager waveManager = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning(
                    "[Zombie War] WaveManager not found in scene. UI will be created; assign WaveManager on WaveAnnouncePresenter when ready.");
            }

            GameObject canvasObject = ensureCanvas();
            Transform canvasTransform = canvasObject.transform;

            UIWaveBossFlashOverlay flash = ensureBossFlash(canvasTransform);
            UIWaveAnnouncePopup wavePopup = ensureAnnouncePopup(
                canvasTransform,
                "ZW_WaveAnnouncePopup",
                isBossVariant: false);
            UIWaveAnnouncePopup bossPopup = ensureAnnouncePopup(
                canvasTransform,
                "ZW_BossAnnouncePopup",
                isBossVariant: true);

            WaveAnnouncePresenter presenter = canvasObject.GetComponent<WaveAnnouncePresenter>();
            if (presenter == null)
            {
                presenter = Undo.AddComponent<WaveAnnouncePresenter>(canvasObject);
            }

            SerializedObject so = new SerializedObject(presenter);
            so.FindProperty("waveManager").objectReferenceValue = waveManager;
            so.FindProperty("wavePopup").objectReferenceValue = wavePopup;
            so.FindProperty("bossPopup").objectReferenceValue = bossPopup;
            so.FindProperty("bossFlashOverlay").objectReferenceValue = flash;
            so.FindProperty("waveAutoHideSeconds").floatValue = 2.2f;
            so.FindProperty("bossAutoHideSeconds").floatValue = 3.8f;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasObject;
            Debug.Log(
                "[Zombie War] Wave announce UI ready.\n" +
                "- Normal wave: ZW_WaveAnnouncePopup\n" +
                "- Boss wave: red flash + ZW_BossAnnouncePopup\n" +
                "- Mark WaveData.IsBoss + DisplayName on LevelWaveConfig.\n" +
                "Presenter on: " + canvasObject.name +
                (waveManager == null ? "\n- WARNING: wire WaveManager reference manually." : string.Empty));
        }

        private static GameObject ensureCanvas()
        {
            GameObject canvasObject = GameObject.Find("ZW_WaveAnnounceCanvas");
            if (canvasObject != null)
            {
                return canvasObject;
            }

            canvasObject = new GameObject(
                "ZW_WaveAnnounceCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Wave Announce Canvas");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            return canvasObject;
        }

        private static UIWaveBossFlashOverlay ensureBossFlash(Transform parent)
        {
            Transform existing = parent.Find("ZW_BossFlashOverlay");
            GameObject root;
            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = new GameObject("ZW_BossFlashOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                Undo.RegisterCreatedObjectUndo(root, "Create Boss Flash Overlay");
                root.transform.SetParent(parent, false);
            }

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.85f, 0.05f, 0.05f, 0.55f);
            image.raycastTarget = false;

            UIWaveBossFlashOverlay flash = root.GetComponent<UIWaveBossFlashOverlay>();
            if (flash == null)
            {
                flash = Undo.AddComponent<UIWaveBossFlashOverlay>(root);
            }

            SerializedObject flashSo = new SerializedObject(flash);
            flashSo.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            flashSo.FindProperty("flashImage").objectReferenceValue = image;
            flashSo.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            root.transform.SetAsFirstSibling();
            return flash;
        }

        private static UIWaveAnnouncePopup ensureAnnouncePopup(Transform parent, string name, bool isBossVariant)
        {
            Transform existing = parent.Find(name);
            GameObject root;
            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(root, "Create Wave Announce Popup");
                root.transform.SetParent(parent, false);
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Transform panelTransform = root.transform.Find("Panel");
            GameObject panelObject;
            if (panelTransform != null)
            {
                panelObject = panelTransform.gameObject;
            }
            else
            {
                panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(panelObject, "Create Announce Panel");
                panelObject.transform.SetParent(root.transform, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.72f);
            panelRect.anchorMax = new Vector2(0.5f, 0.72f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(780f, 210f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = isBossVariant
                ? new Color(0.45f, 0.05f, 0.05f, 0.94f)
                : new Color(0.08f, 0.1f, 0.14f, 0.92f);
            panelImage.raycastTarget = false;

            TMP_Text title = ensureTmp(panelObject.transform, "Title", isBossVariant ? "BOSS WAVE" : "WAVE 1", 64);
            TMP_Text subtitle = ensureTmp(panelObject.transform, "Subtitle", isBossVariant ? "Prepare for the boss!" : "Incoming!", 32);

            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.05f, 0.42f);
            titleRect.anchorMax = new Vector2(0.95f, 0.92f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.color = isBossVariant ? new Color(1f, 0.25f, 0.25f, 1f) : Color.white;
            title.fontStyle = FontStyles.Bold;

            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.05f, 0.08f);
            subtitleRect.anchorMax = new Vector2(0.95f, 0.42f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            subtitle.color = new Color(1f, 1f, 1f, 0.85f);

            GameObject normalRoot = ensureChild(panelObject.transform, "NormalRoot");
            GameObject bossRoot = ensureChild(panelObject.transform, "BossRoot");
            normalRoot.SetActive(!isBossVariant);
            bossRoot.SetActive(isBossVariant);

            UIWaveAnnouncePopup popup = root.GetComponent<UIWaveAnnouncePopup>();
            if (popup == null)
            {
                popup = Undo.AddComponent<UIWaveAnnouncePopup>(root);
            }

            SerializedObject popupSo = new SerializedObject(popup);
            popupSo.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            popupSo.FindProperty("panel").objectReferenceValue = panelRect;
            popupSo.FindProperty("titleText").objectReferenceValue = title;
            popupSo.FindProperty("subtitleText").objectReferenceValue = subtitle;
            popupSo.FindProperty("normalVisualRoot").objectReferenceValue = normalRoot;
            popupSo.FindProperty("bossVisualRoot").objectReferenceValue = bossRoot;
            popupSo.FindProperty("panelBackground").objectReferenceValue = panelImage;
            popupSo.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return popup;
        }

        private static GameObject ensureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Create " + name);
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return child;
        }

        private static TMP_Text ensureTmp(Transform parent, string name, string text, float fontSize)
        {
            Transform existing = parent.Find(name);
            GameObject textObject;
            if (existing != null)
            {
                textObject = existing.gameObject;
            }
            else
            {
                textObject = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(textObject, "Create TMP " + name);
                textObject.transform.SetParent(parent, false);
            }

            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = Undo.AddComponent<TextMeshProUGUI>(textObject);
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            return tmp;
        }
    }
}
#endif
