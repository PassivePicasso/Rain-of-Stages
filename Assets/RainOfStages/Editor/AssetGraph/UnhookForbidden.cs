using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

using System.Linq;
using System.IO;
using static UnityEditor.EditorGUILayout;

namespace PassivePicasso.RainOfStages.Editor
{

    public class UnhookForbidden 
    {

        [SerializeField] private string ForbiddenPath;
        private string RenderedPath => Path.Combine("Assets", ForbiddenPath);

        public void OnValidate()
        {
            Debug.Log($"UnhookForbidden.OnValidate");

        }

        //// Test if asset is different from intended configuration 
        //public bool IsModified(UnityEngine.Object[] assets, List<AssetReference> group)
        //{
        //    Debug.Log($"UnhookForbidden.IsModified");
        //    var forbidden = assets.Select(AssetDatabase.GetAssetPath).SelectMany(AssetDatabase.GetDependencies).Any(path => path.StartsWith(RenderedPath));
        //    return forbidden;
        //}


        //// Actually change asset configurations. 
        //public void Modify(UnityEngine.Object[] assets, List<AssetReference> group)
        //{
        //    Debug.Log($"UnhookForbidden.Modify");
        //}

        // Draw inspector gui 
        public void OnInspectorGUI(Action onValueChanged)
        {

            EditorGUILayout.HelpBox("This is the inspector of your custom Modifier. You can customize by implementing OnInspectorGUI().", MessageType.Info);
            using (new HorizontalScope())
            {
                GUILayout.Label("Assets/");
                GUILayout.Label(ForbiddenPath);
            }
            using (new HorizontalScope())
            {
                if (GUILayout.Button("Select Folder"))
                {
                    string path = EditorUtility.OpenFolderPanel("Open Game Executable", "Assets", "");
                    if (string.IsNullOrEmpty(path)) return;

                    string oldValue = Path.Combine(Directory.GetCurrentDirectory(), "Assets").Replace("\\", "/");
                    string cleanedPath = path.Replace("\\", "/");
                    ForbiddenPath = cleanedPath.Replace(oldValue, string.Empty).TrimStart('/', '\\').TrimEnd('/', '\\');
                    onValueChanged();
                }
                if (GUILayout.Button("Ping Folder"))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RenderedPath));
            }
        }
    }
}