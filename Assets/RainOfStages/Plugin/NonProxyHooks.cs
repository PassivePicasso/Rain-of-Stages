using BepInEx.Logging;
using PassivePicasso.RainOfStages.Monomod;
using PassivePicasso.RainOfStages.Proxy;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PassivePicasso.RainOfStages.Hooks
{
    using Links = IEnumerable<Link>;
    using RainOfStages = Plugin.RainOfStages;
    using SceneDefRefs = IEnumerable<SceneDefReference>;
    using SceneDefs = IEnumerable<SceneDefinition>;
    internal class NonProxyHooks
    {
        static Material mat = new Material(Shader.Find("Unlit/Color"));
        static ManualLogSource Logger => RainOfStages.Instance.RoSLog;
        static IReadOnlyList<string> GameModeNames => RainOfStages.Instance.GameModeNames;

        struct DrawOrder { public Vector3 start; public Vector3 end; public float duration; public Color color; }

        static Dictionary<Guid, DrawOrder> DrawOrders = new Dictionary<Guid, DrawOrder>();

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
                string[] skip = new[] { "Classic", "ClassicRun" };
                var gameModes = RainOfStages.Instance.GameModes.Where(gm => !skip.Contains(gm.name));
                foreach (var gameMode in gameModes)
                {
                    var copied = Transform.Instantiate(weeklyButton);
                    copied.name = $"GenericMenuButton ({gameMode})";
                    GameObject.DestroyImmediate(copied.GetComponent<DisableIfGameModded>());

                    var tmc = copied.GetComponent<LanguageTextMeshController>();
                    tmc.token = gameMode.GetComponent<Run>().nameToken;

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


}