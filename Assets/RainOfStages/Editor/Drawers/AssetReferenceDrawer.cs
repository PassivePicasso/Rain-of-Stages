using PassivePicasso.RainOfStages.Plugin.AssetMapping;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(WeakAssetReferenceAttribute), true)]
    public class AssetReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var assetReferenceAttribute = (WeakAssetReferenceAttribute)attribute;
            var type = assetReferenceAttribute.Type;
            
            var guid = property.stringValue;
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);

            var newAsset = EditorGUI.ObjectField(position, label, asset, type, true);

            var newGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newAsset));
            property.stringValue = newGuid;
        }
    }
}