#if THUNDERKIT_CONFIGURED
using BepInEx;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Shared
{
    [BepInPlugin("com.PassivePicasso.RainOfStages.Shared", "RainOfStages.Shared", "1.1.2")]
    public class LibraryLoad : BaseUnityPlugin
    {
        public AssetBundle RoSShared { get; private set; }
        private void Awake()
        {
            LoadAssetBundles();
        }

        private void LoadAssetBundles()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var workingDirectory = Path.GetDirectoryName(assemblyLocation);
            var file = new FileInfo(Path.Combine(workingDirectory, "rosshared.manifest"));
            var directory = file.DirectoryName;
            var filename = Path.GetFileNameWithoutExtension(file.FullName);
            var abmPath = Path.Combine(directory, filename);
            RoSShared = AssetBundle.LoadFromFile(abmPath);
        }
    }
}
#endif
