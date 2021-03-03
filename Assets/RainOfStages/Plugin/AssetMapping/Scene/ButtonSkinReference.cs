using RoR2.UI.SkinControllers;
using System.Reflection;

namespace PassivePicasso.RainOfStages.Behaviours
{
    public class ButtonSkinReference : SceneAssetMapper<ButtonSkinController, RoR2.UI.UISkinData>
    {
        protected override string MemberName => "skinData";

        protected override BindingFlags FieldBindings => BindingFlags.Public | BindingFlags.Instance;
    }
}
