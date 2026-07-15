using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[InitializeOnLoad]
public static class TimeScaleToolbar
{
    static float min = 0f;
    static float max = 3f;

    static TimeScaleToolbar()
    {
        ToolbarExtender.LeftToolbarGUI.Add(Draw);
    }

    static void Draw()
    {
        GUILayout.Space(10);

        if (GUILayout.Button("1x", GUILayout.Width(30)))
        {
            Time.timeScale = 1f;
        }
        GUILayout.Label("Time", GUILayout.Width(35));

        float newScale = GUILayout.HorizontalSlider(Time.timeScale, min, max, GUILayout.Width(50));
        if (Mathf.Abs(newScale - Time.timeScale) > 0.001f)
        {
            Time.timeScale = newScale;
        }

        GUILayout.Label(Time.timeScale.ToString("0.0"), GUILayout.Width(40));

    }
}
#endif