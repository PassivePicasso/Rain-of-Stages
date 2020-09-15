#if THUNDERKIT_CONFIGURED
using PassivePicasso.RainOfStages.Editor.DataPreProcessors;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor
{
    public abstract class ScriptablePopupDrawer<T, DPP> : PropertyDrawer where T : ScriptableObject where DPP : DataPreProcessor<T, DPP>, new()
    {
        public static DPP Processor { get; private set; }
        static ScriptablePopupDrawer()
        {
            if (Processor == null)
                Processor = new DPP();

            EditorApplication.update += Processor.UpdateSelectionsCache;
            Processor.UpdateSelectionsCache();
        }

        public virtual string ModifyName(string name) => name;
        public virtual string ModifyLabel(string label) => label;
        public virtual string CategoryName(SerializedProperty property, GUIContent label) => label.text;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            List<string> names = new List<string>();

            names = Processor[CategoryName(property, label)].Select(ms => ms.name).ToList();
            var pickedCategory = Processor[CategoryName(property, label)].ToArray();

            if (pickedCategory == null)
            {
                base.OnGUI(position, property, label);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.LabelField(position, ModifyLabel(label.text));

            position = new Rect(position.x + EditorGUIUtility.labelWidth,
                                position.y,
                                position.width - EditorGUIUtility.labelWidth,
                                position.height);

            string currentSelection = property.objectReferenceValue?.name;
            var selectedIndex = names.IndexOf(currentSelection);
            int newIndex = -1;

            newIndex = EditorGUI.Popup(position,
                                       selectedIndex,
                                       names.Select(name => ObjectNames.NicifyVariableName(ModifyName(name)))
                                            .ToArray());

            EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex > -1)
                    property.objectReferenceValue = pickedCategory[newIndex];
                else
                    property.objectReferenceValue = null;

                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
