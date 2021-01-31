using BepInEx;
using BepInEx.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Shared
{
    [BepInPlugin(Constants.GuidName, Constants.Name, Constants.Version)]
    public class LibraryLoad : BaseUnityPlugin
    {
        public AssetBundle RoSShared { get; private set; }
        public new ManualLogSource Logger => base.Logger;
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