#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FullscreenGameView
{
    static int DISPLAY_0 = 0; // typical gameview display
    static int DISPLAY_1 = 1; // display gameview doesn't render to
    static EditorWindow _instance;
    static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
    static readonly PropertyInfo ShowToolbarProperty = GameViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

    static void SetGameViewTargetDisplay( int displayIndex)
    {
        EditorWindow gameView = GetMainGameView();
        System.Type type = gameView.GetType();
        type.InvokeMember(
            "SetTargetDisplay",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
            null,
            gameView,
            new object[] { displayIndex }
            );
    }

    [MenuItem("Tools/Fullscreen Game View (Toggle) _F11", priority = 2)]
    public static void Toggle()
    {
        if (GameViewType == null)
        {
            Debug.LogError("GameView type not found.");
            return;
        }
        if (ShowToolbarProperty == null)
        {
            Debug.LogWarning("GameView.showToolbar property not found.");
        }
        if (_instance != null)
        {
            _instance.Close();
            _instance = null;
            SetGameViewTargetDisplay(DISPLAY_0);
        }
        else
        {
            SetGameViewTargetDisplay(DISPLAY_1);
            _instance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);
            ShowToolbarProperty?.SetValue(_instance, false);
            Vector2 position = new Vector2(0, 0); // Vector2.zero
            Vector2 resolution = new Vector2(1920, 1080);
            resolution /= EditorGUIUtility.pixelsPerPoint;
            var fullscreenRect = new Rect(position, resolution);
            _instance.ShowPopup();
            _instance.position = fullscreenRect;
            _instance.Focus();
        }
    }

    public static EditorWindow GetMainGameView()
    {
        System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
        {
            Debug.LogError("Unable to find the UnityEditor.GameView type.");
            return null;
        }
        return EditorWindow.GetWindow(gameViewType);
    }

}
#endif
