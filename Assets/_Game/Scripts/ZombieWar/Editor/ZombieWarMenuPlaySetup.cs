#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.UI;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Wires Menu.unity Play button for Loading → Menu → Gameplay flow.
    /// Menu: Zombie War/Setup Menu Play Button
    /// </summary>
    public static class ZombieWarMenuPlaySetup
    {
        private const string MenuPath = "Zombie War/Setup Menu Play Button";
        private const string MenuScenePath = "Assets/_SDK/Template/Scenes/Menu.unity";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            Button button = Object.FindFirstObjectByType<Button>();
            if (button == null)
            {
                Debug.LogError("[Zombie War] No Button found in Menu scene.");
                return;
            }

            button.gameObject.name = "PlayButton";

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "PLAY — LEVEL 1";
                label.fontSize = Mathf.Max(label.fontSize, 48);
                label.alignment = TextAnchor.MiddleCenter;
            }

            ZombieWarMenuPlayButton play = button.GetComponent<ZombieWarMenuPlayButton>();
            if (play == null)
            {
                play = Undo.AddComponent<ZombieWarMenuPlayButton>(button.gameObject);
            }

            SerializedObject so = new SerializedObject(play);
            so.FindProperty("playButton").objectReferenceValue = button;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("consumeLife").boolValue = false;
            so.FindProperty("forceStartLevel1OnFirstPlay").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Stretch button a bit for touch.
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.25f, 0.42f);
                rect.anchorMax = new Vector2(0.75f, 0.58f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = button.gameObject;

            Debug.Log(
                "[Zombie War] Menu Play ready.\n" +
                "Flow: Loading → Menu → PLAY → Gameplay (profile LEVEL)\n" +
                "Win → NEXT LEVEL advances LEVEL and reloads Gameplay.");
        }
    }
}
#endif
