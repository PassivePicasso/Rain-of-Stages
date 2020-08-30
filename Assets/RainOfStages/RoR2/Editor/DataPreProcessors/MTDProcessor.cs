using PassivePicasso.ThunderKit.Proxy;
using RoR2;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor.DataPreProcessors
{
    public class MTDProcessor : MonoBehaviour
    {
        public static string[] CategorySelectionGuids;
        public static MusicTrackDefRef[] MusicTracks;

        static int waitTime = 10;
        static float elapsed = 0;

        private static void UpdateSelectionsCache()
        {
            string[] set = AssetDatabase.FindAssets("t:MusicTrackDefRef");

            elapsed += Time.deltaTime;
            if (elapsed < waitTime) return;
            elapsed = 0;

            CategorySelectionGuids = set;

            var paths = set.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

            MusicTracks = paths.Select(path => AssetDatabase.LoadAssetAtPath<MusicTrackDefRef>(path)).ToArray();

        }

        [InitializeOnLoadMethod()]
        static void Initialize()
        {
            EditorApplication.update += UpdateSelectionsCache;
            UpdateSelectionsCache();
        }
    }
}