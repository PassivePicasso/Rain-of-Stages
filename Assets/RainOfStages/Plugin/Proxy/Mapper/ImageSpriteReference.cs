#if THUNDERKIT_CONFIGURED
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RainOfStages.Behaviours
{
    public class ImageSpriteReference : SceneAssetMapper<Image, Sprite>
    {
        protected override string MemberName => "m_Sprite";

        protected override BindingFlags FieldBindings => BindingFlags.NonPublic | BindingFlags.Instance;
    }
}
#endif
