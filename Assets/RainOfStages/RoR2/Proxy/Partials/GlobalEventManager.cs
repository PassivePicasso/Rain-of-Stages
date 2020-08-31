#if THUNDERKIT_CONFIGURED
using UnityEngine;

namespace PassivePicasso.ThunderKit.Proxy.RoR2
{
    public partial class GlobalEventManager
    {
        void Awake()
        {
            AACannonMuzzleEffect = (GameObject)Resources.Load("prefabs/effects/muzzleflashes/muzzleflashaacannon");
            AACannonPrefab = (GameObject)Resources.Load("prefabs/projectiles/aacannon");
            chainLightingPrefab = (GameObject)Resources.Load("prefabs/projectiles/chainlightning");
            daggerPrefab = (GameObject)Resources.Load("prefabs/projectiles/daggerprojectile");
            explodeOnDeathPrefab = (GameObject)Resources.Load("prefabs/networkedobjects/willowispdelay");
            healthOrbPrefab = (GameObject)Resources.Load("prefabs/networkedobjects/healthglobe");
            missilePrefab = (GameObject)Resources.Load("prefabs/projectiles/missileprojectile");
            plasmaCorePrefab = (GameObject)Resources.Load("prefabs/projectiles/plasmacore");
        }
    }
}
#endif