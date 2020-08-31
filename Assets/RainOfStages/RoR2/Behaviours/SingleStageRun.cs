#if THUNDERKIT_CONFIGURED
using UnityEngine;

namespace PassivePicasso.RainOfStages.Runs
{
    public class SingleStageRun : ThunderKit.Proxy.RoR2.Run
    {
        public RoR2.SceneDef requiredScene;
        public override void AdvanceStage(RoR2.SceneDef nextScene) => base.AdvanceStage(requiredScene);

    }
}
#endif