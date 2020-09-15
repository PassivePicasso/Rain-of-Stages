#if THUNDERKIT_CONFIGURED
using UnityEngine;
using UnityEditor;

namespace RainOfStages.Assets.RainOfStages.RoR2.Editor.Drawers
{
    using MonsterFamily = global::RoR2.ClassicStageInfo.MonsterFamily;

    [CustomPropertyDrawer(typeof(MonsterFamily), true)]
    public class MonsterFamilyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var monsterFamilyCategories = property.FindPropertyRelative(nameof(MonsterFamily.monsterFamilyCategories));
            var familySelectionChatString = property.FindPropertyRelative(nameof(MonsterFamily.familySelectionChatString));
            var selectionWeight = property.FindPropertyRelative(nameof(MonsterFamily.selectionWeight));
            var minimumStageCompletion = property.FindPropertyRelative(nameof(MonsterFamily.minimumStageCompletion));
            var maximumStageCompletion = property.FindPropertyRelative(nameof(MonsterFamily.maximumStageCompletion));

            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginProperty(position, label, property);

            var localPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(localPosition, monsterFamilyCategories);
            StepPosition(ref localPosition);

            EditorGUI.LabelField(localPosition, "Selection Token");
            StepPosition(ref localPosition, 0, EditorGUIUtility.labelWidth);
            familySelectionChatString.stringValue = EditorGUI.TextField(localPosition, familySelectionChatString.stringValue);
            StepPosition(ref localPosition, EditorGUIUtility.singleLineHeight, -EditorGUIUtility.labelWidth);

            EditorGUI.LabelField(localPosition, "Selection Weight");
            StepPosition(ref localPosition, 0, EditorGUIUtility.labelWidth);
            selectionWeight.floatValue = EditorGUI.FloatField(localPosition, selectionWeight.floatValue);
            StepPosition(ref localPosition, EditorGUIUtility.singleLineHeight, -EditorGUIUtility.labelWidth);

            EditorGUI.LabelField(localPosition, "Stage Range");

            StepPosition(ref localPosition, 0, EditorGUIUtility.labelWidth);
            var halfWidth = localPosition.width / 2;
            localPosition = new Rect(localPosition.x, localPosition.y, halfWidth, localPosition.height);
            minimumStageCompletion.intValue = EditorGUI.IntField(localPosition, minimumStageCompletion.intValue);

            localPosition = new Rect(localPosition.x + halfWidth, localPosition.y, halfWidth, localPosition.height);
            maximumStageCompletion.intValue = EditorGUI.IntField(localPosition, maximumStageCompletion.intValue);

            EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.SetIsDifferentCacheDirty();
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight * 4) + EditorGUIUtility.standardVerticalSpacing;
        }

        void StepPosition(ref Rect position) => StepPosition(ref position, EditorGUIUtility.singleLineHeight, 0);
        void StepPosition(ref Rect position, float verticalIncrement, float horizontalIncrement) => position = new Rect(position.x + horizontalIncrement, position.y + verticalIncrement, position.width - horizontalIncrement, EditorGUIUtility.singleLineHeight);
    }
}
#endif
