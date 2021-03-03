using PassivePicasso.RainOfStages.Plugin.AssetMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PassivePicasso.RainOfStages.Plugin.AssetMapping
{
    [ExecuteAlways]
    public abstract class AssetArrayMapper<ComponentType, AssetType> : MonoBehaviour where AssetType : UnityEngine.Object where ComponentType : Component
    {
        public virtual AssetType[] ClonedAssets => Array.Empty<AssetType>();

        public IEnumerable<AssetType> Asset { get; private set; }

        [WeakAssetReference(typeof(Material))]
        public string[] EditorAssets;

        /// <summary>
        /// Full scene path of target asset 
        /// </summary>
        public string SourceAssetPath;

        public ComponentType[] TargetComponents;

        private Action<ComponentType, IEnumerable<AssetType>> setValue;
        protected virtual BindingFlags FieldBindings { get; } = BindingFlags.Public | BindingFlags.Instance;
        protected abstract string MemberName { get; }

#if !UNITY_EDITOR
        private void Start()
        {
            Initialize();
            Assign(Asset);
            Destroy(this);
        }
#else
        void OnEnable()
        {
            Initialize();
            Assign(ClonedAssets);
        }

        void OnDisable()
        {
            Initialize();
            Assign(Enumerable.Empty<AssetType>());
        }
#endif

        protected abstract ComponentType GetTargetComponent();
        void Initialize()
        {
            if (setValue != null) return;
            
            var componentField = typeof(ComponentType).GetField(MemberName, FieldBindings);
            Type assetType = typeof(AssetType);

            if (componentField != null)
            {
                bool isFieldAssignable = assetType.IsAssignableFrom(componentField.FieldType.GetElementType());
                if (isFieldAssignable)
                {
                    setValue = (component, asset) => componentField.SetValue(component, asset);
                    var targetComponent = GetTargetComponent();
                    if (targetComponent)
                        Asset = (IEnumerable<AssetType>)componentField.GetValue(targetComponent);
                    return;
                }
            }
            var componentProperty = typeof(ComponentType).GetProperty(MemberName, FieldBindings);
            if (componentProperty != null)
            {
                bool isPropertyAssignable = assetType.IsAssignableFrom(componentProperty.PropertyType.GetElementType());
                if (isPropertyAssignable)
                {
                    setValue = (component, asset) => componentProperty.SetValue(component, asset);
                    var targetComponent = GetTargetComponent();
                    if (targetComponent)
                        Asset = (IEnumerable<AssetType>)componentProperty.GetValue(targetComponent);
                    return;
                }
            }
        }

        void Assign(IEnumerable<AssetType> assets)
        {
            if (setValue == null || TargetComponents == null || TargetComponents.Length == 0) return;

            foreach (var component in TargetComponents.Where(tc => tc))
            {
                setValue(component, assets);
            }
        }
    }
}
