using BepInEx.Logging;
using PassivePicasso.RainOfStages.Monomod;
using PassivePicasso.RainOfStages.Proxy;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PassivePicasso.RainOfStages.Hooks
{
    using RainOfStages = Plugin.RainOfStages;
    internal class ModdingHooks
    {
        static ManualLogSource Logger => RainOfStages.Instance.RoSLog;
        private static FieldInfo[] sceneDefFields = typeof(SceneDef).GetFields(BindingFlags.Public | BindingFlags.Instance);

        [Hook(typeof(RoR2.Networking.SteamLobbyFinder), isStatic: true)]
        private static void CCSteamQuickplayStart(Action<ConCommandArgs> orig, ConCommandArgs args)
        {
            Debug.Log("Possible hacking attempt reported to VAC");
        }

        [Hook(typeof(RoR2.Console))]
        private static void InitConVars(Action<RoR2.Console> orig, RoR2.Console self)
        {
            Logger.LogMessage("Intercepting InitConVars");
            orig(self);

            var convar = self.FindConVar("gamemode");
            convar.flags = ConVarFlags.ExecuteOnServer;
        }

        [Hook(typeof(DisableIfGameModded))]
        public static void OnEnable(Action<DisableIfGameModded> orig, DisableIfGameModded self) => self.gameObject.SetActive(false);

        [Hook(typeof(SceneDef))]
        private static void Awake(Action<SceneDef> orig, SceneDef self)
        {
            if (self is SceneDefReference sdr)
            {
                var def = Resources.Load<SceneDef>($"SceneDefs/{sdr.name}");
                foreach (var field in sceneDefFields)
                    field.SetValue(self, field.GetValue(def));
            }
            orig(self);
        }

        //Disable vote Selector
        //[Hook(typeof(CharacterSelectController), "Start")]
        private static void CharacterSelectController_Start(Action<CharacterSelectController> orig, CharacterSelectController self)
        {
            orig(self);
            try
            {
                Logger.LogMessage("Adding Run Selector to Main Menu");

                var campaingselectorbundle = RainOfStages.OtherBundles.First(bundle => bundle.name.Equals("campaingselector"));
                var campaignSelectorPrefab = campaingselectorbundle.LoadAllAssets<GameObject>().First();

                var campaignSelector = GameObject.Instantiate(campaignSelectorPrefab);
                var selectorTransform = campaignSelector.GetComponent<RectTransform>();

                var content = GameObject.Find("RuleBookViewerVertical").GetComponentInChildren<ContentSizeFitter>().transform;

                selectorTransform.SetParent(content, false);
            }
            catch (Exception e)
            {
                Logger.LogError("Error Adding Run Selector to Main Menu");
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
            }
            finally
            {
                Logger.LogMessage("Finished Main Menu Modifications");
            }
        }


        static IReadOnlyList<string> GameModeNames => RainOfStages.Instance.GameModeNames;
        [Hook(typeof(MainMenuController), "Start")]
        public static void MainMenuController_Start(Action<MainMenuController> orig, MainMenuController self)
        {
            orig(self);
            try
            {
                Logger.LogMessage("Adding GameModes to Alternate GameMOdes menu");

                var weeklyButton = GameObject.Find("GenericMenuButton (Weekly)");
                var juicedPanel = weeklyButton.transform.parent;


                foreach (var gameMode in GameModeNames)
                {
                    var copied = GameObject.Instantiate(weeklyButton);
                    copied.transform.parent = juicedPanel;
                    GameObject.DestroyImmediate(copied.GetComponent<RoR2.DisableIfGameModded>());
                    copied.SetActive(true);

                    var tmc = copied.GetComponent<LanguageTextMeshController>();
                    tmc.token = gameMode;

                    var consoleFunctions = copied.GetComponent<RoR2.ConsoleFunctions>();

                    copied.GetComponent<HGButton>()
                          .onClick.AddListener(() => consoleFunctions.SubmitCmd($"transition_command \"set_scene {gameMode}\""));
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error Adding Run Selector to Main Menu");
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
            }
            finally
            {
                Logger.LogMessage("Finished Main Menu Modifications");
            }

        }
    }
    internal class SceneCatalogHooks
    {
        static ManualLogSource Logger => RainOfStages.Instance.RoSLog;

        [Hook(typeof(SceneCatalog), isStatic: true)]
        private static void Init(Action orig)
        {
            orig();
            HookAttribute.DisableHooks(typeof(SceneCatalogHooks));

            var lookups = SceneCatalog.allSceneDefs.ToDictionary(sd => sd.baseSceneName);

            Logger.LogInfo("Lodded dictionary for sceneNameOverride doping");

            var sceneDefinitions = RainOfStages.Instance.SceneDefinitions.OfType<SceneDefinition>();

            {
                var maps = sceneDefinitions.SelectMany(destination => destination.destionationInjections.Select(origin => (destination, origin)));
                var mapGroups = maps.GroupBy(map => map.origin.baseSceneName);

                foreach (var mapGroup in mapGroups)
                {
                    var destinations = lookups[mapGroup.Key].destinations = mapGroup.Select(map => map.destination as SceneDef).ToArray();
                    foreach (var destination in destinations)
                        Logger.LogMessage($"Added destination {destination.baseSceneName} to SceneDef {mapGroup.Key}");
                }
            }

            {
                var maps = sceneDefinitions.SelectMany(overridingScene => overridingScene.reverseSceneNameOverrides.Select(overridedScene => (overridingScene, overridedScene)));
                var mapGroups = maps.GroupBy(map => map.overridedScene.baseSceneName);

                foreach (var mapGroup in mapGroups)
                {
                    var overridingScenes = lookups[mapGroup.Key].sceneNameOverrides = mapGroup.Select(map => map.overridingScene.baseSceneName).ToList();

                    foreach (var overridingScene in overridingScenes)
                        Logger.LogMessage($"Added override {overridingScene} to SceneDef {mapGroup.Key}");
                }
            }
        }
    }

}