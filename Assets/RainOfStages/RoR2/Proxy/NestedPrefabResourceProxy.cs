#if THUNDERKIT_CONFIGURED
using UnityEngine;

namespace PassivePicasso.RainOfStages.Proxy
{
    public class NestedPrefabResourceProxy : MonoBehaviour
    {
        public string RootPrefabPath;
        public string PrefabPath;

        // Start is called before the first frame update
        void Start()
        {
            var root = Resources.Load<GameObject>(RootPrefabPath);
            var prefab = root.transform.Find(PrefabPath);
            var instance = Instantiate(prefab);
            instance.position = transform.position;
            instance.rotation = transform.rotation;
            instance.localScale = transform.localScale;
            instance.SetParent(transform.parent);
            Destroy(gameObject);
        }
    }
}
#endif
