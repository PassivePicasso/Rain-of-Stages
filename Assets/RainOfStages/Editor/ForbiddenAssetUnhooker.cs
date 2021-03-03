using PassivePicasso.RainOfStages.Utilities;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor
{
    public class ForbiddenAssetUnhooker : ScriptableObject, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var settings = ScriptableHelper.EnsureAsset<ForbiddenAssetUnhooker>("Assets/RainOfStages/BuildProcessorData.asset", (pp) => { });
            Debug.Log("post-test");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = ScriptableHelper.EnsureAsset<ForbiddenAssetUnhooker>("Assets/RainOfStages/BuildProcessorData.asset", (pp) => { });
            Debug.Log("pre-test");
        }
    }
}