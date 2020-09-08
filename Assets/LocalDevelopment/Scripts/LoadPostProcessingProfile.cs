using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.PostProcessing;
using System.Linq;

namespace RainOfStages.Assets.LocalDevelopment.Scripts
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class LoadPostProcessingProfile : MonoBehaviour
    {
        public string ProfileName;
        // Use this for initialization
        void Awake()
        {
            var profiles = Object.FindObjectsOfType<PostProcessProfile>();
            var profile = profiles.FirstOrDefault(p => p.name.Equals(ProfileName));
            var volume = GetComponent<PostProcessVolume>();
            //volume.profile = profile;
            volume.sharedProfile = profile;
            //Destroy(this);
        }
    }
}