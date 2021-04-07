using System;
using System.Linq;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Plugin.AssetMapping
{
    public class ResourceMaterialMapper : ResourceAssetArrayMapper<MeshRenderer, Material>
    {
        protected override string MemberName => "materials";

#if UNITY_EDITOR
        public override Material[] ClonedAssets =>
                    EditorAssets
                    .Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x))
                    .Select(x => UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(x))
                    .Select(Instantiate)
                    .Select(clone =>
                    {
                        clone.hideFlags = HideFlags.HideAndDontSave;
                        clone.name = clone.name.Replace("(Clone)", "(WeakAssetReference)");
                        return clone;
                    })
                    .ToArray();
#endif
    }
}
