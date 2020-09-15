#if THUNDERKIT_CONFIGURED
using UnityEngine;
using UnityEditor;

namespace PassivePicasso.RainOfStages.Editor
{
    public class DefaultGameObjects : ScriptableObject
    {
        public GameObject Director;
        public GameObject GlobalEventManager;
        public GameObject SceneInfo;
    }
}
#endif
