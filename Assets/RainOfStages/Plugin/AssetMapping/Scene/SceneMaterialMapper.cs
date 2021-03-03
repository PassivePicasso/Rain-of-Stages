using System;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Plugin.AssetMapping
{
    public class SceneMaterialMapper : SceneAssetArrayMapper<MeshRenderer, Material>
    {
        public override Material[] ClonedAssets => Array.Empty<Material>();
        protected override string MemberName => "materials";
    }
}
