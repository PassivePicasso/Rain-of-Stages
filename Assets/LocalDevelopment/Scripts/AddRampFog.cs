using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace PassivePicasso.RainOfStages.Variants
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class AddRampFog : MonoBehaviour
    {
        public PostProcessProfile profile;

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

        bool hasUpdated = false;
        private void Start()
        {
            profile?.AddSettings(new RampFog
            {
                enabled = new BoolParameter { overrideState = true, value = true },
                
                fogIntensity = intensity,
                fogPower = power,
                fogZero = zero,
                fogOne = one,
                
                fogHeightStart = heightStart,
                fogHeightEnd = heightEnd,
                fogHeightIntensity = heightIntensity,
                
                skyboxStrength = skyboxStrength,

                fogColorStart = colorStart,
                fogColorMid = colorMid,
                fogColorEnd = colorEnd,
            });
            GetComponent<PostProcessVolume>().enabled = false;
            hasUpdated = false;
        }

        private void Update()
        {
            if (hasUpdated) return;

            GetComponent<PostProcessVolume>().sharedProfile = profile;
            GetComponent<PostProcessVolume>().enabled = true;

            hasUpdated = true;
        }
    }
}