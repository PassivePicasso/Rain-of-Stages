using UnityEngine;
using UnityEditor;
using System.Linq;
using RoR2;
using System.Collections.Generic;
using PassivePicasso.RainOfStages.Editor.DataPreProcessors;

namespace PassivePicasso.RainOfStages.Editor
{
    [CustomPropertyDrawer(typeof(MusicTrackDef), true)]
    public class MusicTrackDefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);
            List<string> names = new List<string>();
            MusicTrackDef[] pickedCategory = null;

            names = MTDProcessor.MusicTracks.Select(ms => ((ScriptableObject)ms).name).ToList();
            pickedCategory = MTDProcessor.MusicTracks;

            if (pickedCategory == null)
            {
                base.OnGUI(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.LabelField(position, label);

            position = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            string currentSelection = property.objectReferenceValue?.name;
            var selectedIndex = names.IndexOf(currentSelection);
            int newIndex = -1;

            newIndex = EditorGUI.Popup(position, selectedIndex, names.Select(name => ObjectNames.NicifyVariableName(name.Substring(2))).ToArray());

            if (newIndex > -1)
                property.objectReferenceValue = pickedCategory[newIndex];
            else
                property.objectReferenceValue = null;

            EditorGUI.EndProperty();

        }
    }
}