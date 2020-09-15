using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace PassivePicasso.RainOfStages.Variants
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class AddRampFog : MonoBehaviour
    {
        public FloatParameter intensity;
        public FloatParameter power;
        public FloatParameter zero;
        public FloatParameter one;
        public FloatParameter heightStart;
        public FloatParameter heightEnd;
        public FloatParameter heightIntensity;
        public ColorParameter colorStart;
        public ColorParameter colorMid;
        public ColorParameter colorEnd;
        public FloatParameter skyboxStrength;
        bool updated = false;

        private void LateUpdate()
        {

            if (updated)
            {
                enabled = false;
                GetComponent<PostProcessVolume>().enabled = true;
            }
            else
            {
                PostProcessVolume postProcessVolume = GetComponent<PostProcessVolume>();
                var profile = ScriptableObject.Instantiate(postProcessVolume.profile);
                profile.name = $"{postProcessVolume.profile.name}(+RampFog)";
                var rampFog = ScriptableObject.CreateInstance<RampFog>();
                rampFog.enabled.Override(true);
                if (intensity      .overrideState) rampFog.fogIntensity      .Override(intensity      .value);
                if (power          .overrideState) rampFog.fogPower          .Override(power          .value);
                if (zero           .overrideState) rampFog.fogZero           .Override(zero           .value);
                if (one            .overrideState) rampFog.fogOne            .Override(one            .value);
                if (heightStart    .overrideState) rampFog.fogHeightStart    .Override(heightStart    .value);
                if (heightEnd      .overrideState) rampFog.fogHeightEnd      .Override(heightEnd      .value);
                if (heightIntensity.overrideState) rampFog.fogHeightIntensity.Override(heightIntensity.value);
                if (skyboxStrength .overrideState) rampFog.skyboxStrength    .Override(skyboxStrength .value);
                if (colorStart     .overrideState) rampFog.fogColorStart     .Override(colorStart     .value);
                if (colorMid       .overrideState) rampFog.fogColorMid       .Override(colorMid       .value);
                if (colorEnd       .overrideState) rampFog.fogColorEnd       .Override(colorEnd       .value);
                rampFog.active = true;

                profile.AddSettings(rampFog);

                postProcessVolume.profile = null;
                postProcessVolume.sharedProfile = profile;
                postProcessVolume.enabled = false;
                updated = true;
            }
        }

    }
}