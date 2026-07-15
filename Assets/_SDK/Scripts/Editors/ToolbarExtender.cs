using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
[InitializeOnLoad]
public static class ToolbarExtender
{
    static Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
    static ScriptableObject currentToolbar;

    public static readonly List<Action> LeftToolbarGUI = new();
    public static readonly List<Action> RightToolbarGUI = new();

    static ToolbarExtender()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (currentToolbar != null)
            return;

        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars.Length == 0)
            return;

        currentToolbar = (ScriptableObject)toolbars[0];

        var root = currentToolbar.GetType()
            .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(currentToolbar) as VisualElement;

        var leftZone = root.Q("ToolbarZonePlayMode");
        var rightZone = root.Q("ToolbarZonePlayMode");

        var leftContainer = new IMGUIContainer(() =>
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI)
                handler();
            GUILayout.EndHorizontal();
        });

        var rightContainer = new IMGUIContainer(() =>
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in RightToolbarGUI)
                handler();
            GUILayout.EndHorizontal();
        });

        leftZone.Insert(0, leftContainer);
        rightZone.Add(rightContainer);
    }
}

#endif