#if THUNDERKIT_CONFIGURED
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RainOfStages.Behaviours
{
    [ExecuteAlways]
    public abstract class SceneAssetMapper<ComponentType, AssetType> : AssetMapper<ComponentType, AssetType> where AssetType : UnityEngine.Object where ComponentType : Component
    {
        protected override ComponentType GetTargetComponent()
        {
            var path = SourceAssetPath.Split('/');
            var targetGameObject = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(go => go.name.Equals(path));
            for (int i = 1; i < path.Length; i++)
            {
                if (!targetGameObject) return null;
                targetGameObject = targetGameObject.transform.Find(path[i])?.gameObject;
            }
            if (!targetGameObject) return null;

            var monoBehaviour = targetGameObject.GetComponent<ComponentType>();
            return monoBehaviour;
        }
    }
}
#endif
