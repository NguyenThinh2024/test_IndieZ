using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
[InitializeOnLoad]
public static class SceneToolbarSwitcher
{
    static string[] scenePaths;
    static string[] sceneNames;
    static int currentIndex;

    static SceneToolbarSwitcher()
    {
        ToolbarExtender.RightToolbarGUI.Add(Draw);
        LoadScenes();
        UpdateCurrentSceneIndex();
    }

    static void LoadScenes()
    {
        List<EditorBuildSettingsScene> enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .ToList();

        scenePaths = enabledScenes
            .Select(scene => scene.path)
            .ToArray();

        sceneNames = enabledScenes
            .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path))
            .ToArray();
    }

    static void UpdateCurrentSceneIndex()
    {
        string currentScene = SceneManager.GetActiveScene().path;

        for (int i = 0; i < scenePaths.Length; i++)
        {
            if (scenePaths[i] == currentScene)
            {
                currentIndex = i;
                return;
            }
        }
    }

    static void Draw()
    {
        LoadScenes();
        UpdateCurrentSceneIndex();

        if (sceneNames == null || sceneNames.Length == 0)
            return;

        GUILayout.Space(15);

        if (GUILayout.Button(new GUIContent("Play First", "Open the first enabled scene from Build Settings Scene List and enter Play Mode."), GUILayout.Width(80)))
        {
            PlayFromFirstScene();
        }

        int newIndex = EditorGUILayout.Popup(currentIndex, sceneNames, GUILayout.Width(140));

        if (newIndex != currentIndex)
        {
            currentIndex = newIndex;
            OpenScene(scenePaths[currentIndex]);
        }
    }

    static void PlayFromFirstScene()
    {
        if (scenePaths == null || scenePaths.Length == 0)
            return;

        if (EditorApplication.isPlaying)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        currentIndex = 0;
        EditorSceneManager.OpenScene(scenePaths[currentIndex]);
        EditorApplication.delayCall += StartPlayMode;
    }

    static void StartPlayMode()
    {
        EditorApplication.delayCall -= StartPlayMode;
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = true;
        }
    }

    static void OpenScene(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path);
    }
}

#endif