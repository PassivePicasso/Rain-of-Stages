#if !THUNDERKIT_CONFIGURED
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Installer
{
    [InitializeOnLoad]
    public class InstallThunderKit
    {
        static InstallThunderKit()
        {
            var current = Assembly.GetExecutingAssembly();
            var location = current.Location;
            Debug.Log(location);
            Client.Add("https://github.com/PassivePicasso/ThunderKit.git");
        }
    }
}
#endif