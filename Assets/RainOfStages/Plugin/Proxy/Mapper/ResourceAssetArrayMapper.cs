#if THUNDERKIT_CONFIGURED
using System.Collections.Generic;
using UnityEngine;

namespace RainOfStages.Behaviours
{
    public abstract class ResourceAssetArrayMapper<ComponentType, AssetType>
                        : AssetArrayMapper<ComponentType, AssetType>
                        where AssetType : IEnumerable<Object>
                        where ComponentType : Component
    {
        public string PrefabPath;

        protected override ComponentType GetTargetComponent()
        {
            var prefab = Resources.Load<GameObject>(SourceAssetPath);
            var foundTransform = prefab.transform.Find(PrefabPath);
            var targetgameObject = foundTransform.gameObject;

            var monoBehaviour = targetgameObject.GetComponent<ComponentType>();
            return monoBehaviour;
        }
       
    }
}
#endif