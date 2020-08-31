#if THUNDERKIT_CONFIGURED
using BepInEx.Logging;
using PassivePicasso.RainOfStages.Monomod;
using PassivePicasso.RainOfStages.Proxy;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PassivePicasso.RainOfStages.Hooks
{
    using RainOfStages = Plugin.RainOfStages;
    internal class NonProxyHooks
    {
        static ManualLogSource Logger => RainOfStages.Instance.RoSLog;
        static IReadOnlyList<string> GameModeNames => RainOfStages.Instance.GameModeNames;

        [Hook(typeof(MainMenuController), "Start")]
        private static void MainMenuController_Start(Action<MainMenuController> orig, MainMenuController self)
        {
            try
            {
                Logger.LogMessage("Adding GameModes to Alternate GameMOdes menu");

                var mainMenu = GameObject.Find("MainMenu")?.transform;
                var weeklyButton = mainMenu.Find("MENU: Extra Game Mode/ExtraGameModeMenu/Main Panel/GenericMenuButtonPanel/JuicePanel/GenericMenuButton (Weekly)");
                Logger.LogDebug($"Found: {weeklyButton.name}");

                var juicedPanel = weeklyButton.transform.parent;

                foreach (var gameMode in GameModeNames.Except(new[] { "Classic", "ClassicRun", "Eclipse", "EclipseRun" }))
                {
                    var copied = Transform.Instantiate(weeklyButton);
                    copied.name = $"GenericMenuButton ({gameMode})";
                    GameObject.DestroyImmediate(copied.GetComponent<DisableIfGameModded>());

                    var tmc = copied.GetComponent<LanguageTextMeshController>();
                    tmc.token = gameMode;

                    var consoleFunctions = copied.GetComponent<ConsoleFunctions>();

                    var hgbutton = copied.gameObject.GetComponent<HGButton>();
                    hgbutton.onClick = new Button.ButtonClickedEvent();

                    hgbutton.onClick.AddListener(() => consoleFunctions.SubmitCmd($"transition_command \"gamemode {gameMode}; host 0;\""));

                    copied.SetParent(juicedPanel);
                    copied.localScale = Vector3.one;
                    copied.gameObject.SetActive(true);
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error Adding GameModes to Alternate GameMOdes menu");
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
            }
            finally
            {
                Logger.LogMessage("Finished Main Menu Modifications");
                orig(self);
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
#endif