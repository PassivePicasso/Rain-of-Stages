#if THUNDERKIT_CONFIGURED
using UnityEngine;

namespace RainOfStages.Behaviours
{
    public class MaterialMapper : ResourceAssetArrayMapper<MeshRenderer, Material[]>
    {
        protected override string MemberName => "materials";
    }
}
#endif