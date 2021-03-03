using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using System.IO;
using System.Text;
using System;

public class AssetBundleInspector : EditorWindow
{
    [MenuItem("Tools/Rain of Stages/AssetBundleInspector")]
    public static void ShowExample()
    {
        AssetBundleInspector wnd = GetWindow<AssetBundleInspector>();
        wnd.titleContent = new GUIContent("AssetBundleInspector");
    }

    [SerializeField] string bundlePath;

    VisualElement AssetList;
    private AssetBundle loadedBundle;

    public void OnEnable()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = this.GetRootVisualContainer();

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath("Assets/RainOfStages/Editor/AssetBundleInspector/AssetBundleInspector.uxml", typeof(VisualTreeAsset)) as VisualTreeAsset;
        VisualElement editorTemplate = visualTree.CloneTree(null);
        root.AddStyleSheetPath("Assets/RainOfStages/Editor/AssetBundleInspector/AssetBundleInspector_style.uss");
        root.Add(editorTemplate);

        var openButton = editorTemplate.Q<Button>("OpenButton");
        AssignClickHandler(openButton, openButton_clicked);

        var unloadButton = editorTemplate.Q<Button>("UnloadButton");
        AssignClickHandler(unloadButton, unloadButton_clicked);

        var scrollElement = new ScrollView();
        scrollElement.AddToClassList("grow");
        AssetList = new VisualElement();
        AssetList.AddToClassList("assetList");
        scrollElement.Add(AssetList);
        root.Add(scrollElement);
    }

    private void unloadButton_clicked()
    {
        AssetBundle.UnloadAllAssetBundles(true);
    }

    private void openButton_clicked()
    {
        AssetList.Clear();
        bundlePath = EditorUtility.OpenFilePanel("Locate AssetBundle", Directory.GetCurrentDirectory(), "*");
        Debug.Log($"bundlePath: {bundlePath}");


        var loadedBundle = AssetBundle.LoadFromFile(bundlePath);
        var assets = loadedBundle.GetAllAssetNames();

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"Found {assets.Length} assets;");
        foreach (var asset in assets)
        {
            logBuilder.AppendLine(asset);
            var button = new Button();
            button.text = asset;
            void SelectAsset()
            {
                var locallyLoadedBundle = AssetBundle.LoadFromFile(bundlePath);
                var loadedAsset = locallyLoadedBundle.LoadAsset(asset);
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(loadedAsset.GetInstanceID(), out string guid, out long fileId))
                    Debug.Log($"Loaded GUID and LocalFileId: {{fileId: {fileId}, guid: {guid}, type: ???}}");

                var clonedAsset = Instantiate(loadedAsset);

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clonedAsset.GetInstanceID(), out guid, out fileId))
                    Debug.Log($"Cloned GUID and LocalFileId: {{fileId: {fileId}, guid: {guid}, type: ???}}");

                var newAssetPath = $"Assets/{locallyLoadedBundle.name}/{asset}";
                locallyLoadedBundle.Unload(true);

                Directory.CreateDirectory(Path.GetDirectoryName(newAssetPath));
                AssetDatabase.CreateAsset(clonedAsset, newAssetPath);
                //AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(loadedAsset), newAssetPath);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(newAssetPath, clonedAsset.GetType());
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            
            AssignClickHandler(button, SelectAsset);
            AssetList.Add(button);
        }
        loadedBundle.Unload(true);

        Debug.Log(logBuilder.ToString());
    }

    private void AssignClickHandler(Button button, Action action)
    {
        if (button.clickable != null)
        {
            button.clickable.clicked -= action;
            button.clickable.clicked += action;
        }
        else
            button.clickable = new Clickable(action);
    }

}