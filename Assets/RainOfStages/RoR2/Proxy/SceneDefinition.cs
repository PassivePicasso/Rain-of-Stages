#if THUNDERKIT_CONFIGURED
using RoR2;
using System.Collections.Generic;

namespace PassivePicasso.RainOfStages.Proxy
{
    public class SceneDefinition : SceneDef
    {
        public List<SceneDefReference> reverseSceneNameOverrides;
        public List<SceneDefReference> destionationInjections;
    }
}
#endif
