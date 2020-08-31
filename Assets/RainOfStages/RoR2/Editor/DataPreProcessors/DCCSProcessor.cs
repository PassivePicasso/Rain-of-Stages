#if THUNDERKIT_CONFIGURED
using RoR2;

namespace PassivePicasso.RainOfStages.Editor.DataPreProcessors
{
    public class DCCSProcessor : DataPreProcessor<DirectorCardCategorySelection, DCCSProcessor>
    {
        public override string Group(DirectorCardCategorySelection instance)
        {
            switch (instance.name)
            {
                case string name when name.Contains("Monsters"):
                    return "Monster Categories";
                case string name when name.Contains("Interactables"):
                    return "Interactable Categories";
                case string name when name.Contains("Family"):
                    return "Monster Family Categories";
            }
            return base.Group(instance);
        }
    }
}
#endif
