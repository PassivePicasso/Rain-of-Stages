#if THUNDERKIT_CONFIGURED
using System;

namespace PassivePicasso.RainOfStages.Plugin
{
    [Serializable]
    public struct Manifest 
    {
        public string name;
        public string version_number;
        public string website_url;
        public string description;
        public string[] dependencies;
    }
}
#endif
