using PassivePicasso.RainOfStages.Plugin.AssetMapping;
#if !UNITY_EDITOR
using System;
#endif
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Behaviours
{
    public class ResourceMaterialMapper : ResourceAssetArrayMapper<MeshRenderer, Material>
    {
        protected override string MemberName => "materials";

        public override Material[] ClonedAssets => 
#if UNITY_EDITOR
                    EditorAssets
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Material>)
                    .Select(Instantiate)
                    .Select(clone =>
                    {
                        clone.hideFlags = HideFlags.HideAndDontSave;
                        clone.name = clone.name.Replace("(Clone)", "(WeakAssetReference)");
                        return clone;
                    })
                    .ToArray();
#else
                    Array.Empty<Material>();
#endif

        [WeakAssetReference(typeof(Material))]
        public string[] EditorAssets;
    }
}
