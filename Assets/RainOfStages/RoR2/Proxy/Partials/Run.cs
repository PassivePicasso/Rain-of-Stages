#if THUNDERKIT_CONFIGURED
using UnityEngine;

namespace PassivePicasso.ThunderKit.Proxy.RoR2
{
    [RequireComponent(typeof(RunArtifactManager))]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkRuleBook))]
    public partial class Run
    {
        public Texture2D previewTexture;
    }
}
#endif