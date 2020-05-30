#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using RainOfStages.Thunderstore;
using UnityEditor.IMGUI.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using EGL = UnityEditor.EditorGUILayout;
using EGU = UnityEditor.EditorGUIUtility;
using GL = UnityEngine.GUILayout;
using System.IO;
using System.IO.Compression;

[CustomEditor(typeof(Manifest))]
public class ManifestEditor : Editor
{
    private const string ROS_Temp = "ros_temp";
    readonly static string TempDir = Path.Combine(Directory.GetCurrentDirectory(), ROS_Temp);
    SearchField searchField;
    string searchString;
    Task<IEnumerable<Package>> SearchTask = Task<IEnumerable<Package>>.FromResult(Enumerable.Empty<Package>());
    int reqPasses = 0;

    List<Package> searchResults;

    private void Initialize()
    {
        searchField = new SearchField();
        searchField.autoSetFocusOnFindCommand = true;
    }

    public override bool RequiresConstantRepaint()
    {
        var isRequired = !SearchTask.IsCompleted;
        if (isRequired && reqPasses > 0) reqPasses--;
        else if (searchResults == null && SearchTask.IsCompleted)
        {
            searchResults = SearchTask.Result.ToList();
            Debug.Log("Found Results: " + searchResults.Count);
            EditorApplication.Step();
        }

        return searchResults == null;
    }

    public override void OnInspectorGUI()
    {
        if (searchField == null) Initialize();

        var manifest = target as Manifest;
        var manifestSo = new SerializedObject(manifest);
        var dependencies = manifestSo.FindProperty("dependencies");

        var rect = EGL.GetControlRect(true, EGU.singleLineHeight);
        EditorGUI.PropertyField(rect, serializedObject.FindProperty("version_number"));

        rect = EGL.GetControlRect(true, EGU.singleLineHeight);
        EditorGUI.PropertyField(rect, serializedObject.FindProperty("website_url"));

        rect = EGL.GetControlRect(true, EGU.singleLineHeight);
        EditorGUI.PropertyField(rect, serializedObject.FindProperty("description"));

        rect = EGL.GetControlRect(true, EGU.singleLineHeight);
        GUI.Label(rect, "Manifest Dependencies");

        rect = EGL.GetControlRect(true, (manifest.dependencies.Count + 1) * EGU.singleLineHeight * 1.5f);

        GUI.Box(rect, "Manifest Dependencies");
        var boxRect = rect;

        for (int i = 0; i < manifest.dependencies.Count; i++)
        {
            var dependencySlot = dependencies.GetArrayElementAtIndex(i);

            var size = new Vector2(boxRect.size.x - EGU.singleLineHeight * 2, EGU.singleLineHeight);
            size = new Vector2(size.x * 1.5f, size.y * 1.5f);
            rect = new Rect(rect.position + Vector2.up * EGU.singleLineHeight, size);

            GUI.Label(rect, dependencySlot.stringValue);


            var buttonSize = new Vector2(EGU.singleLineHeight * 2, EGU.singleLineHeight);
            var buttonPosition = new Rect(boxRect.position.x + boxRect.size.x - buttonSize.x,
                                          rect.position.y, 25, EGU.singleLineHeight);
            if (GUI.Button(buttonPosition, "x"))
            {
                var dependencyPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Dependencies", dependencySlot.stringValue);
                if (Directory.Exists(dependencyPath))
                    Directory.Delete(dependencyPath, true);

                dependencies.DeleteArrayElementAtIndex(i);

                dependencies.serializedObject.SetIsDifferentCacheDirty();

                dependencies.serializedObject.ApplyModifiedProperties();


                AssetDatabase.Refresh();
            }
         }

        rect = EGL.GetControlRect(true, EGU.singleLineHeight);

        var labelRect = new Rect(rect.position,
                        new Vector2(EGU.labelWidth, EGU.singleLineHeight));

        var fieldRect = new Rect(rect.position + Vector2.right * EGU.labelWidth,
                        rect.size - Vector2.right * EGU.labelWidth);

        GUI.Label(labelRect, "Dependency Search");



        searchString = searchField.OnGUI(fieldRect, searchString);

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return && searchField.HasFocus())
        {
            SearchTask = ThunderLoad.LookupPackage(searchString, isCaseSensitive: false);
            searchResults = null;

            reqPasses = 10;
        }

        if (string.IsNullOrEmpty(searchString)) searchResults = null;

        if (SearchTask.IsCompleted && searchResults != null)
        {
            EGL.BeginVertical();

            foreach (var result in searchResults)
            {
                if (manifest.dependencies.Contains(result.full_name)) continue;
                if (GL.Button(result.name))
                {
                    var dependencySlot = dependencies.GetArrayElementAtIndex(dependencies.arraySize++);
                    dependencySlot.stringValue = result.full_name;

                    string filePath = Path.Combine(TempDir, $"{result.full_name}.zip");
                    var download = ThunderLoad.DownloadPackageAsync(result, filePath);

                    download.ContinueWith(t =>
                    {
                        var dependencyPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Dependencies", result.name);
                        if (Directory.Exists(dependencyPath))
                            Directory.Delete(dependencyPath, true);

                        Directory.CreateDirectory(dependencyPath);

                        using (var fileStream = File.OpenRead(filePath))
                        using (var archive = new ZipArchive(fileStream))
                            archive.ExtractToDirectory(Path.Combine(dependencyPath));

                        AssetDatabase.Refresh();
                    });

                    dependencies.serializedObject.SetIsDifferentCacheDirty();

                    dependencies.serializedObject.ApplyModifiedProperties();
                }
            }

            EGL.EndVertical();
        }
    }
}
#endif