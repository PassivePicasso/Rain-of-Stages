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
    using Links = IEnumerable<Link>;
    using SceneDefs = IEnumerable<SceneDefinition>;
    using SceneDefRefs = IEnumerable<SceneDefReference>;
    internal class NonProxyHooks
    {
        static Material mat = new Material(Shader.Find("Unlit/Color"));
        static ManualLogSource Logger => RainOfStages.Instance.RoSLog;
        static IReadOnlyList<string> GameModeNames => RainOfStages.Instance.GameModeNames;

        struct DrawOrder { public Vector3 start; public Vector3 end; public float duration; public Color color; }

        static Dictionary<Guid, DrawOrder> DrawOrders = new Dictionary<Guid, DrawOrder>();

        [Hook(typeof(ContentManager), isStatic: true)]
        public static void SetContentPacks(Action<List<ContentPack>> orig, List<ContentPack> contentPacks)
        {
            RainOfStages.Instance.RoSLog.LogMessage("Intercepting Content Pack Load");
            RainOfStages.Instance.RoSLog.LogMessage("Evaluating existing Content Packs");
            contentPacks.Add(new ContentPack
            {
                sceneDefs = RainOfStages.Instance.SceneDefinitions.ToArray(),
                gameModePrefabs = RainOfStages.Instance.GameModes.ToArray()
            });
            orig(contentPacks);

            var contentPackFields = typeof(ContentPack).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var pack in contentPacks)
            {
                RainOfStages.Instance.RoSLog.LogDebug($"ContentPack: {pack.GetType().Name}");
                foreach (var field in contentPackFields)
                {
                    var fieldData = field.GetValue(pack);
                    Array dataArray = (Array)fieldData;
                    RainOfStages.Instance.RoSLog.LogDebug($"({field.FieldType.Name}) {field.Name}: {dataArray.Length}");
                }
            }
        }

        public static void InitializeDebugging()
        {
            Camera.onPostRender += OnDrawOrders;
        }

        [Hook(typeof(Debug), isStatic: true)]
        public static void DrawLine(Action<Vector3, Vector3, Color, float, bool> orig, Vector3 start, Vector3 end, Color color, float duration = 0.0f, bool depthTest = true)
        {
            DrawOrders[Guid.NewGuid()] = new DrawOrder { start = start, end = end, duration = duration, color = color };
        }

        protected static void OnDrawOrders(Camera camera)
        {
            List<DrawOrder> orders = new List<DrawOrder>();
            var guids = DrawOrders.Keys.ToArray();
            foreach (var guid in guids)
            {
                var order = DrawOrders[guid];

                order.duration -= Time.deltaTime * 0.25f;
                if (order.duration <= 0)
                    DrawOrders.Remove(guid);
                else
                {
                    DrawOrders[guid] = order;
                    orders.Add(order);
                }
            }
            DrawLines(orders);
        }

        static void DrawLines(List<DrawOrder> drawOrders)
        {
            if (drawOrders.Count == 0) return;

            for (int o = 0; o < drawOrders.Count; o++)
            {
                GL.Begin(GL.LINES);
                GL.Color(drawOrders[o].color);

                GL.Vertex(drawOrders[o].start);
                GL.Vertex(drawOrders[o].end);
                GL.End();
            }
        }

        [Hook(typeof(MainMenuController), "Start")]
        private static void MainMenuController_Start(Action<MainMenuController> orig, MainMenuController self)
        {
            try
            {
                Logger.LogDebug("Adding GameModes to ExtraGameModeMenu menu");

                var mainMenu = GameObject.Find("MainMenu")?.transform;
                var weeklyButton = mainMenu.Find("MENU: Extra Game Mode/ExtraGameModeMenu/Main Panel/GenericMenuButtonPanel/JuicePanel/GenericMenuButton (Weekly)");
                Logger.LogDebug($"Found: {weeklyButton.name}");

                var juicedPanel = weeklyButton.transform.parent;
                string[] skip = new[] { "Classic", "ClassicRun", "Eclipse", "EclipseRun" };
                var gameModes = RainOfStages.Instance.GameModes.Where(gm => !skip.Contains(gm.name));
                foreach (var gameMode in gameModes)
                {
                    var copied = Transform.Instantiate(weeklyButton);
                    copied.name = $"GenericMenuButton ({gameMode})";
                    GameObject.DestroyImmediate(copied.GetComponent<DisableIfGameModded>());

                    var tmc = copied.GetComponent<LanguageTextMeshController>();
                    tmc.token = gameMode.nameToken;

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
                Logger.LogError("Error Adding GameModes to ExtraGameModeMenu menu");
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
            }
            finally
            {
                Logger.LogInfo("Finished Main Menu Modifications");
                orig(self);
            }
        }
    }

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

            var overrideMapping = MakeLinks(sceneDefinitions, def => def.reverseSceneNameOverrides);
            var destinationsMapping = MakeLinks(sceneDefinitions, def => def.destionationInjections);

            Weave(lookups, overrideMapping, 
                       sd => sd.Destination.baseSceneName,
                       sd => sd.sceneNameOverrides,
                       (sd, data) => sd.sceneNameOverrides = data.ToList());

            Weave(lookups, destinationsMapping, 
                       sd => sd.Destination,
                       sd => sd.destinations, 
                       (sd, data) => sd.destinations = data.ToArray());
        }

        static Links MakeLinks(SceneDefs definitions, Func<SceneDefinition, SceneDefRefs> selectSource) 
               => definitions.SelectMany(def => selectSource(def).Select(sdr => new Link(def, sdr)));

        static void Weave<T>(Dictionary<string, SceneDef> lookups, Links links, Func<Link, T> GetNewData, Func<SceneDef, IEnumerable<T>> GetAssignedData, Action<SceneDef, IEnumerable<T>> assignData)
        {
            foreach (var mapGroup in links.GroupBy(map => map.Origin.baseSceneName))
            {
                var newData = mapGroup.Select(GetNewData);
                var oldData = GetAssignedData(lookups[mapGroup.Key]);
                var updatedData = oldData.Union(newData);
                assignData(lookups[mapGroup.Key], updatedData);

                foreach (var dataElement in updatedData)
                    Logger.LogInfo($"Added {dataElement} to SceneDef {mapGroup.Key}");
            }
        }

    }
}