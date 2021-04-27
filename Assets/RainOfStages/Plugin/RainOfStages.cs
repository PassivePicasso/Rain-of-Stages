#pragma warning disable 618
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HG;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;

namespace PassivePicasso.RainOfStages.Plugin
{
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

        public static string Hash(byte[] bytes)
        {
            using (var md5 = MD5.Create())
            {
                var shortNameHash = md5.ComputeHash(bytes);
                var guid = new Guid(shortNameHash);
                var cleanedGuid = guid.ToString().ToLower().Replace("-", "");
                return cleanedGuid;
            }
        }

        void LoadAssetBundles(DirectoryInfo dir, LoadStaticContentAsyncArgs args, ref float progress)
        {
            RoSLog.LogDebug($"SimpleLoading: {dir.FullName}");
            var bundles = Directory.EnumerateFiles(dir.FullName, "*.ros", SearchOption.AllDirectories).ToArray();
            var progressStep = .8f / bundles.Length;
            var hashSet = new HashSet<string>();
            foreach (var file in bundles)
            {
                try
                {
                    RoSLog.LogDebug($"Loading bundle: {Path.GetFileName(file)}");
                    var directory = Path.GetDirectoryName(file);
                    var bundle = AssetBundle.LoadFromFile(file);
                    var fileInfo = new FileInfo(file);
                    var bytesToRead = (int)Mathf.Clamp(fileInfo.Length, 0, 1024 * 1024 * 100);

                    byte[] bytes = new byte[bytesToRead];
                    using (var stream = new BufferedStream(File.OpenRead(file), bytesToRead))
                        stream.Read(bytes, 0, bytesToRead);

                    var mapGuid = $"{bundle.name}@{Hash(bytes)}";
                    NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append(mapGuid);
                    RoSLog.LogInfo($"[NetworkCompatibility] Adding Map Guid: {mapGuid}");

                    if (bundle.isStreamedSceneAssetBundle)
                        sceneBundles.Add(bundle);
                    else
                        AssetBundles.Add(bundle);

                    progress += progressStep;
                    args.ReportProgress(progress);
                }
                catch (Exception e)
                {
                    RoSLog.LogError($"Didn't load {file}\r\n\r\n{e.Message}\r\n\r\n{e.StackTrace}");
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
            ContentManager.onContentPacksAssigned += WeaveDefinitions;
            args.ReportProgress(1f);
            yield break;
        }

        private void WeaveDefinitions(ReadOnlyArray<ReadOnlyContentPack> obj)
        {
            ContentManager.onContentPacksAssigned -= WeaveDefinitions;

            var uniqueSceneDefs = ContentManager.allLoadedContentPacks.SelectMany(rocp => rocp.sceneDefs).Distinct().ToArray();
            RainOfStages.Instance.RoSLog.LogMessage($"{ContentManager.allLoadedContentPacks.Length} ContentPacks found");
            RainOfStages.Instance.RoSLog.LogMessage($"{uniqueSceneDefs.Length} SceneDefs found in all loaded ContentPacks");

            var lookups = uniqueSceneDefs.ToDictionary(sd => sd.cachedName);

            var sceneDefinitions = uniqueSceneDefs.OfType<SceneDefinition>().ToList();

            foreach (var sceneDefinition in sceneDefinitions)
            {
                for (int i = 0; i < sceneDefinition.reverseSceneNameOverrides.Count; i++)
                {
                    var defRef = sceneDefinition.reverseSceneNameOverrides[i];
                    var targetDef = lookups[defRef.cachedName];

                    targetDef.sceneNameOverrides.Add(sceneDefinition.cachedName);
                }

                for (int i = 0; i < sceneDefinition.destinationInjections.Count; i++)
                {
                    var defRef = sceneDefinition.destinationInjections[i];
                    var targetDef = lookups[defRef.cachedName];

                    var destinations = targetDef.destinations.ToList();
                    destinations.Add(sceneDefinition);
                    targetDef.destinations = destinations.Distinct().ToArray();
                }
            }

            RainOfStages.Instance.RoSLog.LogMessage($"{lookups.Count} lookups found");

            foreach (var def in uniqueSceneDefs)
            {
                for (int i = 0; i < def.destinations.Length; i++)
                    if (def.destinations[i] is SceneDefReference sdr && lookups.ContainsKey(sdr.cachedName))
                        def.destinations[i] = lookups[sdr.cachedName];
            }
        }

        #endregion
    }
}