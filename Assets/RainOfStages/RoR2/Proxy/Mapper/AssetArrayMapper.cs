#if THUNDERKIT_CONFIGURED
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace RainOfStages.Behaviours
{
    [ExecuteAlways]
    public abstract class AssetArrayMapper<ComponentType, AssetType> : MonoBehaviour where AssetType : IEnumerable<UnityEngine.Object> where ComponentType : Component
    {
        public AssetType Asset { get; private set; }
        public ComponentType Component { get; private set; }

        /// <summary>
        /// Full scene path of target asset 
        /// </summary>
        [Tooltip("Full path of Asset relative to a Scene File, / separated")]
        public string SourceAssetPath;

        public ComponentType[] TargetComponents;

        private Action<ComponentType> setValue;
        protected virtual BindingFlags FieldBindings { get; } = BindingFlags.Public | BindingFlags.Instance;
        protected abstract string MemberName { get; }

        private void Start()
        {
            if (Application.isEditor)
            {
                //Debug.Log(typeof(ComponentType).FullName);

                //var fields = typeof(ComponentType).GetFields(FieldBindings);
                //var fieldNames = fields.Select(f => f.Name).ToArray().Aggregate((a, b) => $"{a}\r\n{b}");
                //Debug.Log(fieldNames);
            }
            else
            {
                Initialize();
                Assign();
            }
        }

        protected abstract ComponentType GetTargetComponent();
        void Initialize()
        {
            Component = GetTargetComponent();
            var componentField = Component.GetType().GetField(MemberName, FieldBindings);
            Type assetType = typeof(AssetType);

            if (componentField != null)
            {
                bool isFieldAssignable = assetType.IsAssignableFrom(componentField.FieldType);
                if (isFieldAssignable)
                {
                    Asset = (AssetType)componentField.GetValue(Component);
                    setValue = (component) => componentField.SetValue(component, Asset);
                    return;
                }
            }
            var componentProperty = Component.GetType().GetProperty(MemberName, FieldBindings);
            if (componentProperty != null)
            {
                bool isPropertyAssignable = assetType.IsAssignableFrom(componentProperty.PropertyType);
                if (isPropertyAssignable)
                {
                    Asset = (AssetType)componentProperty.GetValue(Component);
                    setValue = (component) => componentProperty.SetValue(component, Asset);
                    return;
                }
            }
        }

        void Assign()
        {
            if (Asset == null || !Asset.Any() || !Component || TargetComponents == null || TargetComponents.Length == 0) return;

            foreach (var component in TargetComponents.Where(tc => tc))
                setValue(component);
        }
    }
}
#endif
