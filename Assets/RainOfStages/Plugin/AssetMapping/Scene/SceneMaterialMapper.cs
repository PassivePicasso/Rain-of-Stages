using System;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Plugin.AssetMapping
{
    public class SceneMaterialMapper : SceneAssetArrayMapper<MeshRenderer, Material>
    {
        protected override string MemberName => "materials";
    }
}
