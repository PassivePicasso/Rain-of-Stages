#if THUNDERKIT_CONFIGURED
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using PassivePicasso.RainOfStages.Hooks;
using PassivePicasso.RainOfStages.Monomod;
using PassivePicasso.RainOfStages.Proxy;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Path = System.IO.Path;
using Run = PassivePicasso.ThunderKit.Proxy.RoR2.Run;

namespace PassivePicasso.RainOfStages.Plugin
{
    //This attribute is required, and lists metadata for your plugin.
    //The GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config). I like to use the java package notation, which is "com.[your name here].[your plugin name here]"

    //The name is the name of the plugin that's displayed on load, and the version number just specifies what version the plugin is.
    [BepInPlugin("com.PassivePicasso.RainOfStages", "RainOfStages", "2.1.3")]
    [BepInDependency("com.PassivePicasso.RainOfStages.Shared", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("R2API", BepInDependency.DependencyFlags.SoftDependency)]
    public class RainOfStages : BaseUnityPlugin
    {
        private const int GameBuild = 4892828;

        class ManifestMap
        {
            public FileInfo File;
            public string[] Content;
            public Manifest manifest;
        }

        public static RainOfStages Instance { get; private set; }

        public static event EventHandler Initialized;

        private static List<AssetBundle> stageManifestBundles;
        private static List<AssetBundle> runManifestBundles;
        private static List<AssetBundle> otherBundles;
        private static List<AssetBundle> sceneBundles;

        public static IReadOnlyList<AssetBundle> StageManifestBundles { get => stageManifestBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> RunManifestBundles { get => runManifestBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> OtherBundles { get => otherBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> SceneBundles { get => sceneBundles.AsReadOnly(); }

        public IReadOnlyList<string> GameModeNames => Instance.gameModeNames.AsReadOnly();
        public IReadOnlyList<Run> GameModes => Instance.gameModes.AsReadOnly();
        public IReadOnlyList<SceneDef> SceneDefinitions => Instance.sceneDefinitions.AsReadOnly();

        private List<Run> gameModes = new List<Run>();
        private List<string> gameModeNames = new List<string> { "ClassicRun" };
        private List<SceneDef> sceneDefinitions = new List<SceneDef>();
        private FieldInfo gameModeCatalogIndexField = typeof(GameModeCatalog).GetField("indexToPrefabComponents", BindingFlags.NonPublic | BindingFlags.Static);
        private List<Manifest> manifests = new List<Manifest>();
        private string[] forbiddenRuns = new[] { "BaseDefenseRun", "WeeklyRun" };
        private bool addedModeEntries;
        private bool countInitialized;

        public ManualLogSource RoSLog => base.Logger;


        public RainOfStages()
        {
            Instance = this;
            RoSLog.LogWarning("Constructor Executed");

            DetourAttribute.Logger = RoSLog;
            HookAttribute.Logger = RoSLog;

            ApplyAttributes();

            if (!Chainloader.PluginInfos.ContainsKey("R2API"))
            {
                RoR2Application.isModded = true;
            }
        }

        #region Messages
        public void Awake()
        {
            RoSLog.LogInfo("Initializing Rain of Stages");
            Initialize();

            var dir = GetPluginsDirectory();

            if (dir == null) throw new ArgumentException(@"invalid plugin path detected, could not find expected ""plugins"" folder in parent tree");

            LoadAssetBundles(dir);

            if (sceneDefinitions.Any())
                RoSLog.LogInfo($"Loaded Scene Definitions: {sceneDefinitions.Select(sd => sd.baseSceneName).Aggregate((a, b) => $"{a}, {b}")}");
            else
                RoSLog.LogInfo($"Loaded no Scene Definitions");

            if (gameModes.Any())
                RoSLog.LogInfo($"Loaded Runs: {gameModes.Select(run => run.name).Aggregate((a, b) => $"{a}, {b}")}");
            else
                RoSLog.LogInfo($"No extra runs loaded");

            Logger.LogInfo($"found {manifests.Count} map manifests");
            if (manifests.Count != 0)
            {
                var hashSet = new HashSet<string>();
                var guids = manifests.Select(manifest => $"{manifest.name};{manifest.version_number}");
                var orderedGuids = guids.OrderBy(value => value);
                var newOrderedGuids = orderedGuids.Where(guid => !NetworkModCompatibilityHelper.networkModList.Contains(guid)).ToArray();
                Logger.LogInfo($"[NetworkCompatibility] Adding to the networkModList\r\n\t{newOrderedGuids.Aggregate((a, b) => $"{a}\r\n\t{b}")}");

                foreach (var mod in newOrderedGuids)
                    NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append(mod);
            }

            Initialized?.Invoke(this, EventArgs.Empty);
        }

        public void Start()
        {
            bool loadGameMode = false;
            string gameMode = string.Empty;
            var loadArg = Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.StartsWith("LoadRun"));
            foreach (var arg in Environment.GetCommandLineArgs())
                switch (arg)
                {
                    case string loadGameModeArg when loadGameModeArg.StartsWith("--LoadGameMode=", StringComparison.OrdinalIgnoreCase):
                        gameMode = loadGameModeArg.Split('=')[1];
                        loadGameMode = true;
                        break;
                }

            if (loadGameMode)
            {
                RoR2.Console.instance.SubmitCmd((NetworkUser)null, "intro_skip 1", false);
                RoR2.Console.instance.SubmitCmd((NetworkUser)null, "splash_skip 1", false);
                RoR2.Console.instance.SubmitCmd((NetworkUser)null, $"transition_command \"gamemode {gameMode}; host 0;\"", false);
            }
        }

        private void Update()
        {
            if (!countInitialized && addedModeEntries)
            {
                var value = gameModeCatalogIndexField.GetValue(null);

                RoR2.Run[] runs = (global::RoR2.Run[])value;

                gameModeNames = runs.Select(r => r.name).Distinct().Where(run => !forbiddenRuns.Contains(run)).ToList();

                RoSLog.LogInfo($"Loaded Custom Runs: {gameModeNames.Aggregate((a, b) => $"{a}, {b}")}");

                countInitialized = true;
            }
        }

        #endregion

        #region Methods

        private void ApplyAttributes()
        {
            HookAttribute.Logger = RoSLog;
            try { HookAttribute.ApplyHooks<SceneCatalogHooks>(); } catch (Exception e) { Logger.LogError(e); }
            try { HookAttribute.ApplyHooks<NonProxyHooks>(); } catch (Exception e) { Logger.LogError(e); }
            try { HookAttribute.ApplyHooks<ThunderKit.Proxy.RoR2.GlobalEventManager>(); } catch (Exception e) { Logger.LogError(e); }

            GameModeCatalog.getAdditionalEntries += ProvideAdditionalGameModes;
            SceneCatalog.getAdditionalEntries += ProvideAdditionalSceneDefs;
        }

        private void Initialize()
        {
            manifests = new List<Manifest>();
            stageManifestBundles = new List<AssetBundle>();
            runManifestBundles = new List<AssetBundle>();
            otherBundles = new List<AssetBundle>();
            sceneBundles = new List<AssetBundle>();
            gameModes = new List<Run>();
            sceneDefinitions = new List<SceneDef>();
        }

        private DirectoryInfo GetPluginsDirectory()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var workingDirectory = Path.GetDirectoryName(assemblyLocation);
            RoSLog.LogInfo(workingDirectory);

            var dir = new DirectoryInfo(workingDirectory);
            while (dir != null && dir.Name != "plugins") dir = dir.Parent;
            return dir;
        }

        private void LoadAssetBundles(DirectoryInfo dir)
        {
            var manifestMaps = dir.GetFiles("*.manifest", SearchOption.AllDirectories)
                                  .Select(manifestFile => new ManifestMap { File = manifestFile, Content = File.ReadAllLines(manifestFile.FullName) })
                                  .Where(mfm => !"rosshared".Equals(mfm.File.Name))
                                  .Where(mfm => mfm.Content.Any(line => line.StartsWith("AssetBundleManifest:")))
                                  .Where(mfm => mfm.Content.Any(line => line.Contains("stagemanifest") || line.Contains("runmanifest")))
                                  .Select(mfm =>
                                  {
                                      var parentDir = Directory.GetParent(mfm.File.DirectoryName);
                                      var manifestPath = string.Empty;
                                      if ("plugins".Equals(Path.GetFileName(parentDir.Name), StringComparison.OrdinalIgnoreCase))
                                          manifestPath = Path.Combine(mfm.File.DirectoryName, "manifest.json");
                                      else
                                          manifestPath = Path.Combine(parentDir.FullName, "manifest.json"); 

                                      var manifest = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : null;
                                      if (!string.IsNullOrEmpty(manifest))
                                          mfm.manifest = JsonUtility.FromJson<Manifest>(manifest);

                                      return mfm;
                                  })
                                  .ToArray();

            RoSLog.LogInfo($"Loaded Rain of Stages compatible AssetBundles");
            foreach (var mfm in manifestMaps)
            {
                try
                {
                    var directory = mfm.File.DirectoryName;
                    var filename = Path.GetFileNameWithoutExtension(mfm.File.FullName);
                    var abmPath = Path.Combine(directory, filename);
                    var namedBundle = AssetBundle.LoadFromFile(abmPath);
                    var manifest = namedBundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
                    var dependentBundles = manifest.GetAllAssetBundles();
                    manifests.Add(mfm.manifest);

                    foreach (var definitionBundle in dependentBundles)
                        try
                        {
                            if (definitionBundle.Contains("rosshared")) continue;
                            var bundlePath = Path.Combine(directory, definitionBundle);
                            var bundle = AssetBundle.LoadFromFile(bundlePath);
                            switch (bundle.name)
                            {
                                case "stagemanifest":
                                    stageManifestBundles.Add(bundle);
                                    break;
                                case "runmanifest":
                                    runManifestBundles.Add(bundle);
                                    break;
                                default:
                                    if (bundle.isStreamedSceneAssetBundle)
                                    {
                                        sceneBundles.Add(bundle);
                                        RoSLog.LogInfo($"Loaded Scene {definitionBundle}");
                                    }
                                    else
                                        otherBundles.Add(bundle);

                                    break;
                            }
                        }
                        catch (Exception e) { RoSLog.LogError(e); }
                }
                catch (Exception e)
                {
                    RoSLog.LogError(e);
                }
            }

            foreach (var bundle in StageManifestBundles)
            {
                var sceneDefinitions = bundle.LoadAllAssets<SceneDefinition>();
                if (sceneDefinitions.Length > 0) this.sceneDefinitions.AddRange(sceneDefinitions);
            }

            foreach (var bundle in RunManifestBundles)
            {
                var customRuns = bundle.LoadAllAssets<GameObject>().Select(go => go.GetComponent<Run>()).Where(run => run != null);
                if (customRuns.Any())
                {
                    gameModes.AddRange(customRuns);
#pragma warning disable 618
                    foreach (var customRun in customRuns)
                        ClientScene.RegisterPrefab(customRun.gameObject);
#pragma warning restore 618
                }
            }
        }

        #endregion

        #region Catalog Injection
        private void ProvideAdditionalGameModes(List<GameObject> obj)
        {
            RoSLog.LogMessage("Loading additional runs");
            obj.AddRange(gameModes.Select(r => r.gameObject));
            addedModeEntries = true;
        }
        private void ProvideAdditionalSceneDefs(List<SceneDef> sceneDefinitions)
        {
            RoSLog.LogMessage("Loading additional scenes");
            sceneDefinitions.AddRange(this.sceneDefinitions);
        }
        #endregion

        private void PrintHieriarchy(Transform transform, int indent = 0)
        {
            string indentString = indent > 0 ? Enumerable.Repeat(" ", indent).Aggregate((a, b) => $"{a}{b}") : "";

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform childTransform = transform.GetChild(i);
                var message = $"{indentString}{childTransform?.gameObject?.name}";
                RoSLog.LogMessage(message);
                PrintHieriarchy(childTransform, indent + 1);
            }
        }
    }
}
#endif