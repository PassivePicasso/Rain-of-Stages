using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PassivePicasso.RainOfStages.Behaviours
{
    [ExecuteAlways]
    public abstract class SceneAssetArrayMapper<ComponentType, AssetType> 
                        : AssetArrayMapper<ComponentType, AssetType> 
                        where AssetType : Object
                        where ComponentType : Component
    {
        protected override ComponentType GetTargetComponent()
        {
            var path = SourceAssetPath.Split('/');
            var scene = SceneManager.GetSceneByName(path[0]);
            var targetGameObject = scene.GetRootGameObjects().FirstOrDefault(go => go.name.Equals(path[1]));
            for (int i = 2; i < path.Length; i++)
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
