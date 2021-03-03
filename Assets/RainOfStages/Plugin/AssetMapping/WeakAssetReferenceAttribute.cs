using System;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Plugin.AssetMapping
{
    public class WeakAssetReferenceAttribute : PropertyAttribute
    {
        public WeakAssetReferenceAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}