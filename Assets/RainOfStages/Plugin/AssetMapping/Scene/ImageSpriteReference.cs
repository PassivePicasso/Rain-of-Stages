using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PassivePicasso.RainOfStages.Plugin.AssetMapping
{
    public class ImageSpriteReference : SceneAssetMapper<Image, Sprite>
    {
        protected override string MemberName => "m_Sprite";

        protected override BindingFlags FieldBindings => BindingFlags.NonPublic | BindingFlags.Instance;
    }
}
