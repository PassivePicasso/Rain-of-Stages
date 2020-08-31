#if THUNDERKIT_CONFIGURED
using UnityEngine;
namespace PassivePicasso.RainOfStages.Proxy
{
    public class ModifiedPrefab : MonoBehaviour
    {
        public string ResourceLocation;
        public bool destroyWhenDone;

        // Use this for initialization
        void Awake()
        {
            var prefab = Resources.Load<GameObject>(ResourceLocation);
            var instance = (GameObject)Instantiate(prefab);
            instance.transform.position = transform.position;

            UpdateComponents(instance.transform);

            if (destroyWhenDone) Destroy(this);
        }


        static void UpdateComponents(Transform transformObject)
        {
            var gameObject = transformObject.gameObject;

            foreach (var component in gameObject.GetComponents(typeof(MonoBehaviour)))
            {
                var componentType = component.GetType();
                if (componentType.Equals(typeof(ModifiedPrefab))) continue;
                var otherComponent = gameObject.GetComponent(componentType) ?? gameObject.AddComponent(componentType);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(component), otherComponent);
            }

            for (int i = 0; i < transformObject.childCount; i++) UpdateComponents(transformObject.GetChild(i));
        }
    }
}
#endif
