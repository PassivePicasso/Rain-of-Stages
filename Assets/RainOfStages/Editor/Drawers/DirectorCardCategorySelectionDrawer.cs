#if THUNDERKIT_CONFIGURED
using PassivePicasso.RainOfStages.Editor.DataPreProcessors;
using RoR2;
using UnityEditor;

namespace PassivePicasso.RainOfStages.Editor
{
    [CustomPropertyDrawer(typeof(DirectorCardCategorySelection), true)]
    public class DirectorCardCategorySelectionDrawer : ScriptablePopupDrawer<DirectorCardCategorySelection, DCCSProcessor>
    {
        public override string ModifyName(string name)
        {
            return base.ModifyName(name).Replace("Interactables", "").Replace("Monsters", "");
        }

        public override string ModifyLabel(string label)
        {
            return base.ModifyLabel(label).Replace("Categories", "");
        }
    }
}
#endif
