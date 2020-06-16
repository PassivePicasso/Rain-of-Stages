using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using MonoMod.RuntimeDetour.HookGen;
using PassivePicasso.RainOfStages.Hooks;
using PassivePicasso.RainOfStages.Proxy;
using PassivePicasso.RainOfStages.UI;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Path = System.IO.Path;
using Run = PassivePicasso.ThunderKit.Proxy.RoR2.Run;

namespace PassivePicasso.RainOfStages.Plugin
{
    //This attribute is required, and lists metadata for your plugin.
    //The GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config). I like to use the java package notation, which is "com.[your name here].[your plugin name here]"

    //The name is the name of the plugin that's displayed on load, and the version number just specifies what version the plugin is.
    [BepInPlugin("com.PassivePicasso.RainOfStages", "RainOfStages", "2020.1.0")]
    [BepInDependency("R2API", BepInDependency.DependencyFlags.SoftDependency)]
    public class RainOfStages : BaseUnityPlugin
    {
        private const int GameBuild = 4892828;

        class ManifestMap
        {
            public FileInfo File;
            public string[] Content;
        }
        private const string NamePrefix = "      Name: ";

        public static RainOfStages Instance { get; private set; }

        public static event EventHandler Initialized;

        public static List<AssetBundle> StageManifestBundles;
        public static List<AssetBundle> RunManifestBundles;
        public static List<AssetBundle> OtherBundles;
        public static List<AssetBundle> SceneBundles;

        public IReadOnlyList<string> RunNames => Instance.runNames.AsReadOnly();
        public IReadOnlyList<Run> Runs => Instance.runs.AsReadOnly();
        public IReadOnlyList<SceneDef> SceneDefinitions => Instance.sceneDefinitions.AsReadOnly();

        private List<Run> runs = new List<Run>();
        private List<string> runNames = new List<string> { "ClassicRun" };
        private List<SceneDef> sceneDefinitions = new List<SceneDef>();
        private FieldInfo gameModeCatalogIndexField = typeof(GameModeCatalog).GetField("indexToPrefabComponents", BindingFlags.NonPublic | BindingFlags.Static);

        private string[] forbiddenRuns = new[] { "BaseDefenseRun", "WeeklyRun" };
        private bool addedModeEntries;
        private bool countInitialized;

        public ManualLogSource RoSLog => base.Logger;

        public RainOfStages()
        {
            Instance = this;
            RoSLog.LogWarning("Constructor Executed");

            DetourAttribute.Logger = RoSLog;

            ApplyAttributes();

            if (!Chainloader.PluginInfos.ContainsKey("R2API"))
            {
                RoR2Application.isModded = true;

                try
                {
                    var consoleRedirectorType = typeof(RoR2.RoR2Application).GetNestedType("UnitySystemConsoleRedirector", BindingFlags.NonPublic);
                    RoSLog.LogMessage($"{consoleRedirectorType.FullName} found in {typeof(RoR2Application).FullName}");
                    var redirect = consoleRedirectorType.GetMethod("Redirect", BindingFlags.Public | BindingFlags.Static);
                    RoSLog.LogMessage($"{redirect.Name}() found in {consoleRedirectorType.FullName}");
                    HookEndpointManager.Add<Action>(redirect, (Action)(() => { }));
                }
                catch (Exception)
                {
                    RoSLog.LogError("Failed to redirect console");
                }

            }
        }

        private void ApplyAttributes()
        {
            HookAttribute.ApplyHooks(typeof(ModdingHooks));
            HookAttribute.ApplyHooks(typeof(SceneCatalogHooks));
            HookAttribute.ApplyHooks(typeof(ConfigureCampaignPanel));

            GameModeCatalog.getAdditionalEntries += ProvideAdditionalRuns;
            SceneCatalog.getAdditionalEntries += ProvideAdditionalSceneDefs;
        }

        //private void OnDisable()
        //{
        //    DetourAttribute.DisableDetours(typeof(RainOfStages));

        //    HookAttribute.DisableHooks(typeof(ConfigureCampaignPanel));
        //    HookAttribute.DisableHooks(typeof(RainOfStages));
        //    HookAttribute.DisableHooks(typeof(ModdingHooks));
        //    HookAttribute.DisableHooks(typeof(SceneCatalogHooks));
        //    HookAttribute.DisableHooks(typeof(QuickPlayButtonControllerHooks));
        //    HookAttribute.DisableHooks(typeof(SceneDefHooks));

        //    GameModeCatalog.getAdditionalEntries -= ProvideAdditionalRuns;
        //    SceneCatalog.getAdditionalEntries -= ProvideAdditionalSceneDefs;
        //}

        #region Messages
        public void Awake()
        {
            RoSLog.LogInfo("Initializing Rain of Stages");
            Initialize();

            //var preGameController = typeof(RoR2Application).Assembly.GetType("RoR2.PreGameController", false);
            //var gameModeConVar = preGameController.GetNestedType("RoR2.GameModeConVar", BindingFlags.NonPublic);
            //var instanceField = gameModeConVar.GetField("instance", BindingFlags.Public | BindingFlags.Static);

            //var gameModeConVarInstance = Activator.CreateInstance(gameModeConVar, "gamemode", ConVarFlags.ExecuteOnServer, "ClassicRun", "Sets the specified game mode as the one to use in the next run.");
            //instanceField.SetValue(null, gameModeConVarInstance);

            //typeof(RoR2.Console).GetField("allConVars", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(RoR2.Console.instance, gameModeConVar);

            var dir = GetPluginsDirectory();

            if (dir == null) throw new ArgumentException(@"invalid plugin path detected, could not find expected ""plugins"" folder in parent tree");

            LoadAssetBundles(dir);
            RoSLog.LogInfo($"Loaded Scene Definitions: {sceneDefinitions.Select(sd => sd.baseSceneName).Aggregate((a, b) => $"{a}, {b}")}");
            RoSLog.LogInfo($"Loaded Custom Runs: {runs.Select(run => run.name).Aggregate((a, b) => $"{a}, {b}")}");


            Initialized?.Invoke(this, EventArgs.Empty);
        }

        private void Update()
        {
            if (!countInitialized && addedModeEntries)
            {
                var value = gameModeCatalogIndexField.GetValue(null);

                RoR2.Run[] runs = (global::RoR2.Run[])value;

                runNames = runs.Select(r => r.name).Distinct().Where(run => !forbiddenRuns.Contains(run)).ToList();

                RoSLog.LogInfo($"Loaded Custom Runs: {runNames.Aggregate((a, b) => $"{a}, {b}")}");

                countInitialized = true;
            }
        }

        #endregion

        #region Methods

        private void Initialize()
        {
            StageManifestBundles = new List<AssetBundle>();
            RunManifestBundles = new List<AssetBundle>();
            OtherBundles = new List<AssetBundle>();
            SceneBundles = new List<AssetBundle>();
            runs = new List<Run>();
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
                                  .Where(mfm => mfm.Content.Any(line => line.StartsWith("AssetBundleManifest:")))
                                  .Where(mfm => mfm.Content.Any(line => line.Contains("stagemanifest") || line.Contains("runmanifest")))
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
                    foreach (var definitionBundle in dependentBundles)
                        try
                        {
                            var bundlePath = Path.Combine(directory, definitionBundle);
                            var bundle = AssetBundle.LoadFromFile(bundlePath);
                            switch (bundle.name)
                            {
                                case "stagemanifest":
                                    StageManifestBundles.Add(bundle);
                                    break;
                                case "runmanifest":
                                    RunManifestBundles.Add(bundle);
                                    break;
                                default:
                                    if (bundle.isStreamedSceneAssetBundle)
                                    {
                                        SceneBundles.Add(bundle);
                                        RoSLog.LogInfo($"Loaded Scene {definitionBundle}");
                                    }
                                    else
                                        OtherBundles.Add(bundle);

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
                if (customRuns.Any()) runs.AddRange(customRuns);
            }
        }

        #endregion

        #region Catalog Injection
        private void ProvideAdditionalRuns(List<GameObject> obj)
        {
            RoSLog.LogMessage("Loading additional runs");
            obj.AddRange(runs.Select(r => r.gameObject));
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