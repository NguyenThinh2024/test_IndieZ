#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieWar.Level;
using ZombieWar.Player;
using ZombieWar.UI;

namespace ZombieWar.EditorTools
{
    public static class ZombieWarWinLoseSceneSetup
    {
        private const string MenuPath = "Zombie War/Setup Win Lose Flow";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            PlayerHealth playerHealth = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            WaveManager waveManager = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
            LevelMapBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<LevelMapBootstrap>();

            if (playerHealth == null)
            {
                Debug.LogError("[Zombie War] PlayerHealth not found. Open Gameplay scene with player__root.");
                return;
            }

            GameObject flowObject = GameObject.Find("ZW_GameFlow");
            if (flowObject == null)
            {
                flowObject = new GameObject("ZW_GameFlow");
                Undo.RegisterCreatedObjectUndo(flowObject, "Create ZW_GameFlow");
            }

            ZombieWarGameFlow gameFlow = flowObject.GetComponent<ZombieWarGameFlow>();
            if (gameFlow == null)
            {
                gameFlow = Undo.AddComponent<ZombieWarGameFlow>(flowObject);
            }

            SerializedObject flowSo = new SerializedObject(gameFlow);
            flowSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            flowSo.FindProperty("waveManager").objectReferenceValue = waveManager;
            flowSo.FindProperty("levelMapBootstrap").objectReferenceValue = bootstrap;
            flowSo.FindProperty("startWavesOnPlay").boolValue = waveManager != null;
            flowSo.FindProperty("waitForMapBeforeStart").boolValue = bootstrap != null;
            flowSo.FindProperty("pauseTimeOnFinish").boolValue = true;
            flowSo.FindProperty("autoCreateResultUi").boolValue = true;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            ensureEventSystem();
            ensureHud(gameFlow, playerHealth, bootstrap);
            ensureTemplateCheatCanvas();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = flowObject;
            Debug.Log(
                "[Zombie War] Win/Lose flow ready.\n" +
                "- Win: clear all spawned zombies after waves finish\n" +
                "- Lose: player health <= 0\n" +
                "- Buttons: Replay + Next Level (win)\n" +
                "- Cheat: Template CheatCanvas (5 clicks)\n" +
                "Object: ZW_GameFlow");
        }

        private static void ensureTemplateCheatCanvas()
        {
            const string cheatPrefabPath = "Assets/_SDK/Template/Scripts/Cheat/Cheat Canvas.prefab";

            GameObject legacy = GameObject.Find("ZW_LevelCheat");
            if (legacy != null)
            {
                Undo.DestroyObjectImmediate(legacy);
            }

            if (UnityEngine.Object.FindFirstObjectByType<Nexzap.Template.CheatCanvas>() != null)
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cheatPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Zombie War] Cheat Canvas prefab missing: {cheatPrefabPath}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add Cheat Canvas");
            instance.name = "Cheat Canvas";
        }

        private static void ensureHud(ZombieWarGameFlow gameFlow, PlayerHealth playerHealth, LevelMapBootstrap bootstrap)
        {
            GameObject canvasObject = GameObject.Find("ZW_ResultHudCanvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("ZW_ResultHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Result HUD Canvas");

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
            }

            Transform canvasTransform = canvasObject.transform;
            GameObject winPanel = ensurePanel(canvasTransform, "WinPanel", "YOU WIN", new Color(0.1f, 0.5f, 0.2f, 0.9f));
            GameObject losePanel = ensurePanel(canvasTransform, "LosePanel", "YOU LOSE", new Color(0.5f, 0.1f, 0.1f, 0.9f));

            Button winReplay = ensureButton(winPanel.transform, "ReplayButton", "REPLAY", new Vector2(0.12f, 0.18f), new Vector2(0.48f, 0.32f));
            Button winNext = ensureButton(winPanel.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(0.52f, 0.18f), new Vector2(0.88f, 0.32f));
            Button loseReplay = ensureButton(losePanel.transform, "ReplayButton", "REPLAY", new Vector2(0.30f, 0.18f), new Vector2(0.70f, 0.32f));

            winPanel.SetActive(false);
            losePanel.SetActive(false);

            ZombieWarHud hud = canvasObject.GetComponent<ZombieWarHud>();
            if (hud == null)
            {
                hud = Undo.AddComponent<ZombieWarHud>(canvasObject);
            }

            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            hudSo.FindProperty("gameFlow").objectReferenceValue = gameFlow;
            hudSo.FindProperty("winPanel").objectReferenceValue = winPanel;
            hudSo.FindProperty("losePanel").objectReferenceValue = losePanel;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject flowSo = new SerializedObject(gameFlow);
            flowSo.FindProperty("winPanel").objectReferenceValue = winPanel;
            flowSo.FindProperty("losePanel").objectReferenceValue = losePanel;
            flowSo.FindProperty("winReplayButton").objectReferenceValue = winReplay;
            flowSo.FindProperty("winNextLevelButton").objectReferenceValue = winNext;
            flowSo.FindProperty("loseReplayButton").objectReferenceValue = loseReplay;
            flowSo.FindProperty("autoCreateResultUi").boolValue = false;
            flowSo.FindProperty("levelMapBootstrap").objectReferenceValue = bootstrap;
            flowSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject ensurePanel(Transform parent, string name, string message, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panel, "Create " + name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;

            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(panel.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.1f, 0.45f);
            labelRect.anchorMax = new Vector2(0.9f, 0.70f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text text = label.GetComponent<Text>();
            text.text = message;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 64;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return panel;
        }

        private static Button ensureButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Button existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                {
                    return existingButton;
                }
            }

            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonObject, "Create " + name);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text text = labelObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 32;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return buttonObject.GetComponent<Button>();
        }

        private static void ensureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }
    }
}
#endif
