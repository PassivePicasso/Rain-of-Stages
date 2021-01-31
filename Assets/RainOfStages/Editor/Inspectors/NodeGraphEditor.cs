#if THUNDERKIT_CONFIGURED
using RoR2;
using RoR2.Navigation;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor
{
    [CustomEditor(typeof(NodeGraph), true)]
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
#endif