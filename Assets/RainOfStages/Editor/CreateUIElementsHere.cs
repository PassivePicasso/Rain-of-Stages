using System;
using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_2019 || UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleEnums;
using Button = UnityEngine.UIElements.Button;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using Button = UnityEngine.Experimental.UIElements.Button;
#endif

namespace PassivePicasso.RainOfStages.Editor
{
    public static class CreateUIElementsHere    {

        [MenuItem("Assets/ThunderKit/UIElements Editor Window")]
        public static void CreateTemplateMenuItem()
        {
            try
            {
                var uiElementsEditorWindowCreator = typeof(EditorWindow).Assembly.GetType("UnityEditor.Experimental.UIElements.UIElementsEditorWindowCreator", true);
                var editorWindow = EditorWindow.GetWindow(uiElementsEditorWindowCreator, true, "UIElements Editor Window Creator");
                editorWindow.maxSize = new Vector2(Styles.K_WindowWidth, Styles. K_WindowHeight);
                editorWindow.minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);

                var selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!AssetDatabase.IsValidFolder(selectionPath))
                    selectionPath = Path.GetDirectoryName(selectionPath);

                uiElementsEditorWindowCreator.GetField("m_Folder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                       .SetValue(editorWindow, selectionPath);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        internal static class Styles
        {
            internal const float K_WindowHeight = 180;
            internal const float K_WindowWidth = 400;
        }
    }
}