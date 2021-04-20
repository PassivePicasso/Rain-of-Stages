using RoR2;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Proxy
{
    public class SurfaceDefProxy : SurfaceDef, IProxyReference<SurfaceDef>
    {
        public SurfaceDef ResolveProxy() => LoadCard<SurfaceDef>();

        private T LoadCard<T>() where T : SurfaceDef
        {
            var card = Resources.Load<T>($"surfacedefs/{name}");
            return card;
        }
    }
}
