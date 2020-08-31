#if THUNDERKIT_CONFIGURED
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RainOfStages.Behaviours
{
    public class ImageSpriteReference : RoSAssetMapper<Image, Sprite>
    {
        protected override string Field => "m_Sprite";

        protected override BindingFlags FieldBindings => BindingFlags.NonPublic | BindingFlags.Instance;
    }
}
#endif
