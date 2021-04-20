#pragma warning disable 618
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using PassivePicasso.RainOfStages.Hooks;
using PassivePicasso.RainOfStages.Monomod;
using PassivePicasso.RainOfStages.Proxy;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;

namespace PassivePicasso.RainOfStages.Plugin
{
    using Links = IEnumerable<Link>;
    using SceneDefRefs = IEnumerable<SceneDefReference>;
    using SceneDefs = IEnumerable<SceneDefinition>;
    struct Link
    {
        public Link(SceneDefinition destination, SceneDefReference origin)
        {
            this.Destination = destination;
            this.Origin = origin;
        }

        public readonly SceneDefinition Destination;
        public readonly SceneDefReference Origin;
    }

    //This attribute is required, and lists metadata for your plugin.
    //The GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config). I like to use the java package notation, which is "com.[your name here].[your plugin name here]"

    //The name is the name of the plugin that's displayed on load, and the version number just specifies what version the plugin is.
    [BepInPlugin(Constants.GuidName, Constants.Name, Constants.Version)]
    [BepInDependency("R2API", BepInDependency.DependencyFlags.SoftDependency)]
    public class RainOfStages : BaseUnityPlugin, IContentPackProvider
    {
        private const int GameBuild = 4892828;

        public static RainOfStages Instance { get; private set; }

        private static List<AssetBundle> stageManifestBundles;
        private static List<AssetBundle> runManifestBundles;
        private static List<AssetBundle> AssetBundles;
        private static List<AssetBundle> sceneBundles;

        public static IReadOnlyList<AssetBundle> StageManifestBundles { get => stageManifestBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> RunManifestBundles { get => runManifestBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> OtherBundles { get => AssetBundles.AsReadOnly(); }
        public static IReadOnlyList<AssetBundle> SceneBundles { get => sceneBundles.AsReadOnly(); }

        public IReadOnlyList<string> GameModeNames => Instance.gameModeNames.AsReadOnly();
        public IReadOnlyList<GameObject> GameModes => Instance.gameModes.AsReadOnly();
        public IReadOnlyList<SceneDef> SceneDefinitions => Instance.sceneDefinitions.AsReadOnly();
        public AssetBundle RoSShared { get; private set; }

        private List<GameObject> gameModes = new List<GameObject>();
        private List<string> gameModeNames = new List<string> { "ClassicRun" };
        private List<SceneDef> sceneDefinitions = new List<SceneDef>();
        private List<Manifest> manifests = new List<Manifest>();

        public ManualLogSource RoSLog => base.Logger;

        public string identifier => Constants.GuidName;

        public static string FastLoadRun { get; private set; } = null;

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
            cheatBackValueField = typeof(RoR2.Console.CheatsConVar).GetField("_boolValue", BindingFlags.NonPublic | BindingFlags.Instance);
            cheatBackValueField.SetValue(RoR2.Console.CheatsConVar.instance, true);
            RoSLog.LogInfo("Initializing Rain of Stages");
            Initialize();
            RoR2Application.onLoad += ProcessCommandLine;
        }

        public void ProcessCommandLine()
        {
            bool loadGameMode = false;
            foreach (var arg in Environment.GetCommandLineArgs())
                switch (arg)
                {
                    case string loadGameModeArg when loadGameModeArg.StartsWith("--LoadGameMode=", StringComparison.OrdinalIgnoreCase):
                        FastLoadRun = loadGameModeArg.Split('=')[1];
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
                SceneManager.activeSceneChanged += LoadRun;
            }
        }

        private void LoadRun(Scene arg0, Scene arg1)
        {
            if (arg1.name == "title")
            {
                SceneManager.activeSceneChanged -= LoadRun;
                Execute($"transition_command \"gamemode {FastLoadRun}; host 0;\"");
            }
        }

        private void Update()
        {
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


        internal void Execute(string cmd)
        {
            RoSLog.LogDebug(cmd);
            RoR2.Console.instance.SubmitCmd((NetworkUser)null, cmd, true);
        }
        private void ApplyAttributes()
        {
            HookAttribute.Logger = RoSLog;
            try { HookAttribute.ApplyHooks<NonProxyHooks>(); } catch (Exception e) { RoSLog.LogError(e); }
            try { HookAttribute.ApplyHooks<Proxy.GlobalEventManager>(); } catch (Exception e) { RoSLog.LogError(e); }
        }

        private void Initialize()
        {
            manifests = new List<Manifest>();
            stageManifestBundles = new List<AssetBundle>();
            runManifestBundles = new List<AssetBundle>();
            AssetBundles = new List<AssetBundle>();
            sceneBundles = new List<AssetBundle>();
            gameModes = new List<GameObject>();
            sceneDefinitions = new List<SceneDef>();

            ContentManager.collectContentPackProviders -= RegisterAsProvider;
            ContentManager.collectContentPackProviders += RegisterAsProvider;
        }

        private void RegisterAsProvider(ContentManager.AddContentPackProviderDelegate addContentPackProvider) => addContentPackProvider(this);

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

        void LoadAssetBundles(DirectoryInfo dir, LoadStaticContentAsyncArgs args, ref float progress)
        {
            RoSLog.LogDebug($"SimpleLoading: {dir.FullName}");
            var bundles = Directory.EnumerateFiles(dir.FullName, "*.ros", SearchOption.AllDirectories).ToArray();
            var progressStep = .8f / bundles.Length;
            foreach (var file in bundles)
            {
                try
                {
                    RoSLog.LogDebug($"Loading bundle: {Path.GetFileName(file)}");
                    var directory = Path.GetDirectoryName(file);
                    var bundle = AssetBundle.LoadFromFile(file);
                    if (bundle.isStreamedSceneAssetBundle)
                    {
                        sceneBundles.Add(bundle);
                    }
                    else
                    {
                        var manifest = bundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");

                        AssetBundles.Add(bundle);

                        var parentDir = Directory.GetParent(directory);
                        var manifestPath = string.Empty;
                        if ("plugins".Equals(Path.GetFileName(parentDir.Name), StringComparison.OrdinalIgnoreCase))
                            manifestPath = Path.Combine(directory, "manifest.json");
                        else
                            manifestPath = Path.Combine(parentDir.FullName, "manifest.json");

                        var tsmanifest = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : null;
                        if (!string.IsNullOrEmpty(tsmanifest))
                            manifests.Add(JsonUtility.FromJson<Manifest>(tsmanifest));
                    }
                    progress += progressStep;
                    args.ReportProgress(progress);
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
                    var sceneDefinitions = assetBundle.LoadAllAssets<SceneDef>().Where(sd => !(sd is SceneDefReference));
                    var gameModes = assetBundle.LoadAllAssets<GameObject>().Where(go => go.GetComponent<Run>());

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

        public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            float progress = 0f;
            LoadRoSShared();
            progress = 0.1f;
            args.ReportProgress(progress);

            var dir = GetPluginsDirectory();

            if (dir == null) throw new ArgumentException(@"invalid plugin path detected, could not find expected ""plugins"" folder in parent tree");

            LoadAssetBundles(dir, args, ref progress);

            if (sceneDefinitions.Any())
                RoSLog.LogInfo($"Loaded Scene Definitions: {sceneDefinitions.Select(sd => sd.baseSceneName).Aggregate((a, b) => $"{a}, {b}")}");
            else
                RoSLog.LogInfo($"Loaded no Scene Definitions");

            if (gameModes.Any())
                RoSLog.LogInfo($"Loaded Runs: {gameModes.Select(run => run.name).Aggregate((a, b) => $"{a}, {b}")}");
            else
                RoSLog.LogInfo($"No extra runs loaded");

            progress = 1f;

            args.ReportProgress(progress);

            yield break;
        }

        public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            args.output.sceneDefs.Add(RainOfStages.Instance.SceneDefinitions.ToArray());

            args.output.gameModePrefabs.Add(RainOfStages.Instance.GameModes.ToArray());

            args.ReportProgress(1f);

            yield break;
        }

        public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            var infos = args.peerLoadInfos;
            
            RainOfStages.Instance.RoSLog.LogMessage($"{infos.Length} ContentPacks found");

            var uniqueSceneDefs = infos.SelectMany(cpli => cpli.previousContentPack.sceneDefs).ToArray();
            RainOfStages.Instance.RoSLog.LogMessage($"{uniqueSceneDefs.Length} SceneDefs found in all loaded ContentPacks");

            var lookups = uniqueSceneDefs.ToDictionary(sd => sd.baseSceneName);

            var sceneDefinitions = RainOfStages.Instance.SceneDefinitions.OfType<SceneDefinition>();

            var overrideMapping = MakeLinks(sceneDefinitions, def => def.reverseSceneNameOverrides);
            var destinationsMapping = MakeLinks(sceneDefinitions, def => def.destinationInjections);

            RainOfStages.Instance.RoSLog.LogMessage($"{lookups.Count} lookups found");
            Weave(lookups, overrideMapping,
                       sd => sd.Destination.baseSceneName,
                       sd => sd.sceneNameOverrides,
                       (sd, data) => sd.sceneNameOverrides = data.ToList());

            Weave(lookups, destinationsMapping,
                       sd => sd.Destination,
                       sd => sd.destinations,
                       (sd, data) => sd.destinations = data.ToArray());

            void Swap(SceneDef[] defs)
            {
                for (int i = 0; i < defs.Length; i++)
                    if (defs[i] is SceneDefReference)
                        defs[i] = uniqueSceneDefs.FirstOrDefault(sd => sd.cachedName == defs[i].cachedName);

            }

            foreach (var contentPack in ContentManager.allLoadedContentPacks)
            {
                foreach (var def in contentPack.sceneDefs)
                {
                    switch (def)
                    {
                        case SceneDefinition sceneDefinition:
                            Swap(sceneDefinition.destinations);
                            Swap(sceneDefinition.destinationInjections.ToArray());
                            Swap(sceneDefinition.reverseSceneNameOverrides.ToArray());
                            break;
                        case SceneDef sceneDef:
                            Swap(sceneDef.destinations);
                            break;
                    }
                }
            }
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
            args.ReportProgress(1f);
            yield break;
        }

        static Links MakeLinks(SceneDefs definitions, Func<SceneDefinition, SceneDefRefs> selectSource)
               => definitions.SelectMany(def => selectSource(def).Select(sdr => new Link(def, sdr)));

        static void Weave<T>(Dictionary<string, SceneDef> lookups, Links links, Func<Link, T> GetNewData, Func<SceneDef, IEnumerable<T>> GetAssignedData, Action<SceneDef, IEnumerable<T>> assignData)
        {
            foreach (var mapGroup in links.GroupBy(map => map.Origin.baseSceneName))
            {
                var newData = mapGroup.Select(GetNewData);
                var key = mapGroup.Key;
                Instance.RoSLog.LogMessage($"Key: {key}");
                var sceneDef = lookups[key];
                Instance.RoSLog.LogMessage($"SceneDef: {sceneDef.baseSceneName}");
                var oldData = GetAssignedData(sceneDef);
                Instance.RoSLog.LogMessage($"oldData: {oldData.Count()}");
                var updatedData = oldData.Union(newData);
                Instance.RoSLog.LogMessage($"updatedData: {updatedData.Count()}");
                assignData(sceneDef, updatedData);

                foreach (var dataElement in updatedData)
                    Instance.RoSLog.LogMessage($"Added {dataElement} to SceneDef {mapGroup.Key}");
            }
        }

        #endregion
    }
}