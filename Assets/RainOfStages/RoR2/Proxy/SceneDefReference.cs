#pragma warning disable 618
#if THUNDERKIT_CONFIGURED
using PassivePicasso.RainOfStages.Monomod;
using RoR2;
using System;
using System.Reflection;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Proxy
{
    public class SceneDefReference : SceneDef
    {
        private static FieldInfo[] sceneDefFields = typeof(SceneDef).GetFields(BindingFlags.Public | BindingFlags.Instance);

        static SceneDefReference()
        {
            if (Application.isEditor) return;
            HookAttribute.ApplyHooks<SceneDefReference>();
        }


        [Hook(typeof(SceneDef), "Awake")]
        private static void HookAwake(Action<SceneDef> orig, SceneDef self)
        {
            if (self is SceneDefReference sdr)
            {
                var def = Resources.Load<SceneDef>($"SceneDefs/{sdr.name}");
                foreach (var field in sceneDefFields)
                    field.SetValue(self, field.GetValue(def));
            }
            orig(self);
        }
    }
}
#endif
