#if THUNDERKIT_CONFIGURED
using PassivePicasso.RainOfStages.Editor.DataPreProcessors;
using PassivePicasso.ThunderKit.Proxy;
using RoR2;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor
{
    [CustomPropertyDrawer(typeof(MusicTrackDef), true)]
    public class MusicTrackDefDrawer : ScriptablePopupDrawer<MusicTrackDefRef, MTDProcessor>
    {
        public override string CategoryName(SerializedProperty property, GUIContent label) => "Default";
    }
}
#endif