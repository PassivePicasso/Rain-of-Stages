using UnityEditor;
using UnityEngine;
namespace PassivePicasso.RainOfStages.Editor
{
    using NG = global::RoR2.Navigation.NodeGraph;
    [CustomEditor(typeof(NG), true)]
    public class NodeGraphEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = new SerializedObject(target);
            GUILayout.Label($"Nodes: {so.FindProperty("nodes").arraySize}");
            GUILayout.Label($"Links: {so.FindProperty("links").arraySize}");
            GUILayout.Label($"Gates: {so.FindProperty("gateNames").arraySize}");
        }
    }
}
