#pragma warning disable 618
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using PassivePicasso.RainOfStages.Hooks;
using PassivePicasso.RainOfStages.Monomod;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Path = System.IO.Path;

namespace PassivePicasso.RainOfStages.Plugin
{
    //This attribute is required, and lists metadata for your plugin.
    //The GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config). I like to use the java package notation, which is "com.[your name here].[your plugin name here]"

    //The name is the name of the plugin that's displayed on load, and the version number just specifies what version the plugin is.
    [BepInPlugin(Constants.GuidName, Constants.Name, Constants.Version)]
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
        private static List<AssetBundle> AssetBundles;
        private static List<AssetBundle> sceneBundles;

        public static IReadOnlyList<AssetBundle> StageManifestBundles { get => stageManifestBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> RunManifestBundles { get => runManifestBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> OtherBundles { get => AssetBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> SceneBundles { get => sceneBundles.AsReadOnly(); }

        public IReadOnlyList<string> GameModeNames => Instance.gameModeNames.AsReadOnly();
        public IReadOnlyList<Run> GameModes => Instance.gameModes.AsReadOnly();
        public IReadOnlyList<SceneDef> SceneDefinitions => Instance.sceneDefinitions.AsReadOnly();
        public AssetBundle RoSShared { get; private set; }

        private List<Run> gameModes = new List<Run>();
        private List<string> gameModeNames = new List<string> { "ClassicRun" };
        private List<SceneDef> sceneDefinitions = new List<SceneDef>();
        private FieldInfo gameModeCatalogIndexField = typeof(GameModeCatalog).GetField("indexToPrefabComponents", BindingFlags.NonPublic | BindingFlags.Static);
        private List<Manifest> manifests = new List<Manifest>();
        private string[] forbiddenRuns = new[] { "BaseDefenseRun", "WeeklyRun" };
        private bool addedModeEntries;
        private bool countInitialized;

        public ManualLogSource RoSLog => base.Logger;

        public bool debugDraw = false;
        public HullClassification debugHull = HullClassification.Human;
        private FieldInfo cheatBackValueField;
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
            LoadRoSShared();

            RoSLog.LogInfo("Initializing Rain of Stages");
            Initialize();

            cheatBackValueField = typeof(RoR2.Console.CheatsConVar).GetField("_boolValue", BindingFlags.NonPublic | BindingFlags.Instance);
            cheatBackValueField.SetValue(RoR2.Console.CheatsConVar.instance, true);

            var dir = GetPluginsDirectory();

            if (dir == null) throw new ArgumentException(@"invalid plugin path detected, could not find expected ""plugins"" folder in parent tree");

            SimpleBundleLoad(dir);
            //LoadAssetBundles(dir);

            if (sceneDefinitions.Any())
                RoSLog.LogInfo($"Loaded Scene Definitions: {sceneDefinitions.Select(sd => sd.baseSceneName).Aggregate((a, b) => $"{a}, {b}")}");
            else
                RoSLog.LogInfo($"Loaded no Scene Definitions");

            if (gameModes.Any())
                RoSLog.LogInfo($"Loaded Runs: {gameModes.Select(run => run.name).Aggregate((a, b) => $"{a}, {b}")}");
            else
                RoSLog.LogInfo($"No extra runs loaded");

            RoSLog.LogInfo($"found {manifests.Count} map manifests");
            if (manifests.Count != 0)
            {
                var hashSet = new HashSet<string>();
                var guids = manifests.Select(manifest => $"{manifest.name};{manifest.version_number}");
                var orderedGuids = guids.OrderBy(value => value);
                var newOrderedGuids = orderedGuids.Where(guid => !NetworkModCompatibilityHelper.networkModList.Contains(guid)).ToArray();

                foreach (var mod in newOrderedGuids)
                {
                    NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append(mod);
                    RoSLog.LogInfo($"[NetworkCompatibility] Adding: {mod}");
                }
            }

            Initialized?.Invoke(this, EventArgs.Empty);

        }

        public void Start()
        {

            bool loadGameMode = false;
            string gameMode = string.Empty;
            foreach (var arg in Environment.GetCommandLineArgs())
                switch (arg)
                {
                    case string loadGameModeArg when loadGameModeArg.StartsWith("--LoadGameMode=", StringComparison.OrdinalIgnoreCase):
                        gameMode = loadGameModeArg.Split('=')[1];
                        loadGameMode = true;
                        break;
                    case string cheatsEnabled when cheatsEnabled.StartsWith("--Debug", StringComparison.OrdinalIgnoreCase):
                        cheatBackValueField.SetValue(RoR2.Console.CheatsConVar.instance, true);
                        var sessionCheatsEnabledProp = typeof(RoR2.Console).GetProperty(nameof(RoR2.Console.sessionCheatsEnabled), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        sessionCheatsEnabledProp.SetValue(null, true);
                        NonProxyHooks.InitializeDebugging();
                        break;
                }

            if (loadGameMode)
            {
                Execute("intro_skip 1");
                Execute("splash_skip 1");
                Execute($"transition_command \"gamemode {gameMode}; host 0;\"");
            }
        }

        private void Update()
        {
            if (!countInitialized && addedModeEntries)
            {
                var value = gameModeCatalogIndexField.GetValue(null);

                Run[] runs = (Run[])value;

                gameModeNames = runs.Select(r => r.name).Distinct().Where(run => !forbiddenRuns.Contains(run)).ToList();

                RoSLog.LogInfo($"Loaded Custom Runs: {gameModeNames.Aggregate((a, b) => $"{a}, {b}")}");

                countInitialized = true;
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                HullClassification next = HullClassification.Count;
                if (debugHull == HullClassification.Count) next = HullClassification.Human;
                else
                {
                    next = (HullClassification)(((int)debugHull) + 1);
                    Execute($"debug_scene_draw_nodegraph 0 {(int)MapNodeGroup.GraphType.Ground} {(int)debugHull}");
                }

                if (next != HullClassification.Count)
                    Execute($"debug_scene_draw_nodegraph 1 {(int)MapNodeGroup.GraphType.Ground} {(int)next}");

                debugHull = next;
            }
        }

        #endregion

        #region Methods


        private void Execute(string cmd)
        {
            RoSLog.LogDebug(cmd);
            RoR2.Console.instance.SubmitCmd((NetworkUser)null, cmd, false);
        }
        private void ApplyAttributes()
        {
            HookAttribute.Logger = RoSLog;
            try { HookAttribute.ApplyHooks<SceneCatalogHooks>(); } catch (Exception e) { RoSLog.LogError(e); }
            try { HookAttribute.ApplyHooks<NonProxyHooks>(); } catch (Exception e) { RoSLog.LogError(e); }
            try { HookAttribute.ApplyHooks<Proxy.GlobalEventManager>(); } catch (Exception e) { RoSLog.LogError(e); }

            GameModeCatalog.getAdditionalEntries += ProvideAdditionalGameModes;
            SceneCatalog.getAdditionalEntries += ProvideAdditionalSceneDefs;
        }

        private void Initialize()
        {
            manifests = new List<Manifest>();
            stageManifestBundles = new List<AssetBundle>();
            runManifestBundles = new List<AssetBundle>();
            AssetBundles = new List<AssetBundle>();
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

        private void LoadRoSShared()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var workingDirectory = Path.GetDirectoryName(assemblyLocation);
            var file = new FileInfo(Path.Combine(workingDirectory, "rosshared.manifest"));
            var directory = file.DirectoryName;
            var filename = Path.GetFileNameWithoutExtension(file.FullName);
            var abmPath = Path.Combine(directory, filename);
            RoSShared = AssetBundle.LoadFromFile(abmPath);
        }

        void SimpleBundleLoad(DirectoryInfo dir)
        {
            RoSLog.LogDebug($"SimpleLoading: {dir.FullName}");
            foreach (var file in Directory.EnumerateFiles(dir.FullName, "*.ros", SearchOption.AllDirectories))
            {
                try
                {
                    RoSLog.LogDebug($"Trying to load bundle: {Path.GetFileName(file)}");
                    var directory = Path.GetDirectoryName(file);
                    var bundle = AssetBundle.LoadFromFile(file);
                    if (bundle.isStreamedSceneAssetBundle)
                    {
                        sceneBundles.Add(bundle);
                        RoSLog.LogInfo($"Loaded Scene {bundle}");
                    }
                    else
                    {
                        RoSLog.LogMessage("Loaded Normie bundle");
                        var manifest = bundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
                        RoSLog.LogMessage("Loaded AssetBundleManifest");

                        AssetBundles.Add(bundle);

                        var parentDir = Directory.GetParent(directory);
                        var manifestPath = string.Empty;
                        if ("plugins".Equals(Path.GetFileName(parentDir.Name), StringComparison.OrdinalIgnoreCase))
                            manifestPath = Path.Combine(directory, "manifest.json");
                        else
                            manifestPath = Path.Combine(parentDir.FullName, "manifest.json");

                        var tsmanifest = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : null;
                        if (!string.IsNullOrEmpty(tsmanifest))
                        {
                            manifests.Add(JsonUtility.FromJson<Manifest>(tsmanifest));
                            RoSLog.LogDebug("Loaded Dependant Manifest");
                        }
                    }
                }
                catch (Exception e)
                {
                    RoSLog.LogDebug($"Didn't load {file}\r\n\r\n{e.Message}\r\n\r\n{e.StackTrace}");
                }
            }

            foreach (var assetBundle in AssetBundles)
            {
                try
                {
                    var sceneDefinitions = assetBundle.LoadAllAssets<SceneDef>();
                    var gameModes = assetBundle.LoadAllAssets<GameObject>().Select(go => go.GetComponent<Run>()).Where(run => run != null);

                    if (sceneDefinitions.Any())
                        this.sceneDefinitions.AddRange(sceneDefinitions);

                    if (gameModes.Any())
                    {
                        this.gameModes.AddRange(gameModes);
                        foreach (var gameMode in gameModes)
                            ClientScene.RegisterPrefab(gameMode.gameObject);
                    }

                }
                catch (Exception e)
                {
                    RoSLog.LogError(e);
                }

            }
        }

        private void LoadAssetBundles(DirectoryInfo dir)
        {
            var manifestFiles = dir.GetFiles("*.manifest", SearchOption.AllDirectories).ToArray();

            RoSLog.LogInfo($"Found {manifestFiles.Length} manifest files");

            var manifestMaps = manifestFiles.Select(manifestFile => new ManifestMap { File = manifestFile, Content = File.ReadAllLines(manifestFile.FullName) })
                                            .Where(mfm => mfm.Content.Any(line => line.Contains("AssetBundleManifest:")))
                                            .ToArray();
            RoSLog.LogInfo($"Created {manifestMaps.Length} manifest maps");

            var loadedManifestMaps = manifestMaps.Select(mfm =>
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
                else
                    mfm.manifest.name = mfm.File.Name;

                return mfm;
            }).ToArray();


            foreach (var loadedManifestMap in loadedManifestMaps)
            {
                RoSLog.LogDebug($"Manifest: {loadedManifestMap.manifest.name}");
                if (loadedManifestMap.manifest.dependencies == null) continue;

                RoSLog.LogDebug($"\t Dependencies: {loadedManifestMap.manifest.dependencies.Length}");
                foreach (var dependency in loadedManifestMap.manifest.dependencies)
                    RoSLog.LogDebug($"\t   - {dependency}");
            }

            var supportedManifests = loadedManifestMaps.Where(mfm => (mfm.manifest.dependencies?.Contains(Constants.DependencyName) ?? false)).ToArray();

            RoSLog.LogInfo($"Loading Rain of Stages compatible AssetBundles");
            foreach (var mfm in supportedManifests)
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
                            if (!File.Exists(bundlePath)) continue;

                            var bundle = AssetBundle.LoadFromFile(bundlePath);

                            if (bundle.isStreamedSceneAssetBundle)
                            {
                                sceneBundles.Add(bundle);
                                RoSLog.LogInfo($"Loaded Scene {definitionBundle}");
                            }
                            else
                                AssetBundles.Add(bundle);
                        }
                        catch (Exception e) { RoSLog.LogError(e); }

                    foreach (var assetBundle in AssetBundles)
                    {
                        try
                        {
                            var sceneDefinitions = assetBundle.LoadAllAssets<SceneDef>();
                            var gameModes = assetBundle.LoadAllAssets<GameObject>().Select(go => go.GetComponent<Run>()).Where(run => run != null);

                            if (sceneDefinitions.Any())
                                this.sceneDefinitions.AddRange(sceneDefinitions);

                            if (gameModes.Any())
                            {
                                this.gameModes.AddRange(gameModes);
                                foreach (var gameMode in gameModes)
                                    ClientScene.RegisterPrefab(gameMode.gameObject);
                            }

                        }
                        catch (Exception e)
                        {
                            RoSLog.LogError(e);
                        }

                    }

                    //foreach (var bundle in stageManifestBundles.Union(runManifestBundles)) bundle.Unload(false);
                }
                catch (Exception e)
                {
                    RoSLog.LogError(e);
                }
            }
        }

        #endregion

        #region Catalog Injection
        private void ProvideAdditionalGameModes(List<GameObject> obj)
        {
            RoSLog.LogInfo("Loading additional runs");
            obj.AddRange(gameModes.Select(r => r.gameObject));
            addedModeEntries = true;
        }
        private void ProvideAdditionalSceneDefs(List<SceneDef> sceneDefinitions)
        {
            RoSLog.LogInfo("Loading additional scenes");
            foreach (var definition in this.sceneDefinitions)
            {
                RoSLog.LogInfo($"Loading: {definition.name}");
                sceneDefinitions.Add(definition);
            }
        }
        #endregion
    }
}