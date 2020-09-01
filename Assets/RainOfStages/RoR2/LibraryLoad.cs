#if THUNDERKIT_CONFIGURED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using System.IO;

namespace PassivePicasso.RainOfStages.Shared
{
    [BepInPlugin("com.PassivePicasso.RainOfStages.Shared", "RainOfStages.Shared", "2020.1.0")]
    public class LibraryLoad : BaseUnityPlugin
    {
        public UnityEngine.Object[] Assets;

        private void Awake()
        {
            LoadAssetBundles();
        }

        private void LoadAssetBundles()
        {
            var file = new FileInfo("rosshared.manifest");
            var directory = file.DirectoryName;
            var filename = Path.GetFileNameWithoutExtension(file.FullName);
            var abmPath = Path.Combine(directory, filename);
            var namedBundle = AssetBundle.LoadFromFile(abmPath);
            Assets = namedBundle.LoadAllAssets();
        }
    }
}
#endif
