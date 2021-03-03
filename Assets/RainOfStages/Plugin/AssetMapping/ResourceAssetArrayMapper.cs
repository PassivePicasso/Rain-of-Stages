using UnityEngine;

namespace PassivePicasso.RainOfStages.Behaviours
{
    public abstract class ResourceAssetArrayMapper<ComponentType, AssetType>
                        : AssetArrayMapper<ComponentType, AssetType>
                        where AssetType : Object
                        where ComponentType : Component
    {
        public string PrefabPath;

        protected override ComponentType GetTargetComponent()
        {
            var prefab = Resources.Load<GameObject>(SourceAssetPath);
            var foundTransform = prefab?.transform.Find(PrefabPath);
            var targetgameObject = foundTransform?.gameObject;

            var monoBehaviour = targetgameObject?.GetComponent<ComponentType>();
            return monoBehaviour;
        }
       
    }
}
