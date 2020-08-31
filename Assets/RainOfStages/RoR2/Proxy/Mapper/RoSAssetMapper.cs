#if THUNDERKIT_CONFIGURED
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace RainOfStages.Behaviours
{
    [ExecuteAlways]
    public abstract class RoSAssetMapper<ComponentType, AssetType> : MonoBehaviour where AssetType : UnityEngine.Object where ComponentType : MonoBehaviour
    {
        public AssetType Asset { get; private set; }
        public ComponentType Component { get; private set; }

        /// <summary>
        /// Full scene path of target asset 
        /// </summary>
        [Tooltip("Full path of Asset relative to a Scene File, / separated")]
        public string SourceAssetPath;

        public ComponentType[] TargetComponents;

        private FieldInfo componentField;
        protected virtual BindingFlags FieldBindings { get; } = BindingFlags.Public | BindingFlags.Instance;
        protected abstract string Field { get; }

        private void Start()
        {
            if (Application.isEditor )
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

        void Initialize()
        {
            var path = SourceAssetPath.Split('/');
            var targetGameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go => go.name.Equals(path[0]));
            for (int i = 1; i < path.Length; i++)
            {
                if (!targetGameObject) return;
                targetGameObject = targetGameObject.transform.Find(path[i])?.gameObject;
            }
            if (!targetGameObject) return;

            var monoBehaviour = targetGameObject.GetComponent<ComponentType>();
            if (!monoBehaviour) return;
            Component = monoBehaviour;

            componentField = monoBehaviour.GetType().GetField(Field, FieldBindings);
            if (!typeof(AssetType).IsAssignableFrom(componentField.FieldType)) return;
            Asset = (AssetType)componentField.GetValue(Component);
        }

        void Assign()
        {
            if (!Asset || componentField == null || !Component || TargetComponents == null || TargetComponents.Length == 0) return;

            foreach (var component in TargetComponents.Where(tc => tc))
                componentField.SetValue(component, Asset);
        }
    }
}
#endif
