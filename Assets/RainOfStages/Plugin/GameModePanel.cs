using BepInEx.Logging;
using PassivePicasso.RainOfStages.Monomod;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PassivePicasso.RainOfStages.UI
{
    using RainOfStages = Plugin.RainOfStages;
    using Run = ThunderKit.Proxy.RoR2.Run;

    public class GameModePanel : NetworkBehaviour
    {
        static readonly Regex gameModeCheck = new Regex(".*gamemode\\s(\\w+);.*", RegexOptions.Compiled | RegexOptions.Singleline);
        static ManualLogSource Logger => RainOfStages.Instance.RoSLog;

        private static GameModePanel instance;

        [SyncVar]
        private int gameModeIndex;

        public Button nextButton;
        public Button previousButton;
        public Image previewImage;
        public TMP_Text campaignTitle;

        public Texture2D defaultimage;

        IReadOnlyList<string> GameModeNames => RainOfStages.Instance.GameModeNames;
        IReadOnlyList<Run> GameModes => RainOfStages.Instance.GameModes;


        // Start is called before the first frame update
        void OnEnable()
        {
            nextButton.onClick.AddListener(NextRun);
            previousButton.onClick.AddListener(PreviousRun);
            UpdateRunSelection(previewImage, campaignTitle);

            GameModePanel.instance = this;
            HookAttribute.ApplyHooks(typeof(GameModePanel));
        }

        private void OnDisable()
        {
            nextButton.onClick.RemoveAllListeners();
            previousButton.onClick.RemoveAllListeners();

            GameModePanel.instance = null;
            HookAttribute.DisableHooks(typeof(GameModePanel));
        }

        [Hook(typeof(RoR2.Console))]
        public static void SubmitCmd(Action<RoR2.Console, NetworkUser, string, bool> orig, RoR2.Console self, NetworkUser sender, string cmd, bool recordSubmit = false)
        {
            orig(self, sender, cmd, recordSubmit);
            if (instance == null) return;

            var match = gameModeCheck.Match(cmd);
            if (match.Success)
            {
                var runName = match.Groups[1].Value;
                var index = instance.GameModeNames.ToList().IndexOf(runName);
                if (index != instance.gameModeIndex)
                    instance.gameModeIndex = index;

                if (instance.gameModeIndex == -1) instance.gameModeIndex = 0;
                instance.UpdateRunSelection(instance.previewImage, instance.campaignTitle);
                if (NetworkServer.active)
                {
                    PreGameController.instance.runSeed = GameModeCatalog.GetGameModePrefabComponent(PreGameController.instance.gameModeIndex).GenerateSeedForNewRun();
                }
            }
        }

        void NextRun()
        {
            if (++gameModeIndex >= GameModeNames.Count) gameModeIndex = 0;
            Send($"gamemode {GameModeNames[gameModeIndex]};");
        }


        void PreviousRun()
        {
            if (--gameModeIndex < 0) gameModeIndex = GameModeNames.Count - 1;
            Send($"gamemode {GameModeNames[gameModeIndex]};");
        }

        void Send(string command)
        {
            ReadOnlyCollection<NetworkUser> localPlayersList = NetworkUser.readOnlyLocalPlayersList;
            NetworkUser sender = (NetworkUser)null;
            if (localPlayersList.Count > 0)
                sender = localPlayersList[0];
            RoR2.Console.instance.SubmitCmd(sender, command, true);
        }

        private void UpdateRunSelection(Image previewImage, TMP_Text textComponent)
        {
            string selectedRun = GameModeNames[gameModeIndex];
            Logger.LogInfo($"Selected Run: {selectedRun}");
            var run = GameModeCatalog.FindGameModePrefabComponent(selectedRun) as Run;
            if (previewImage)
            {
                previewImage.preserveAspect = true;
                Texture2D previewTexture = null;
                if (run && run.previewTexture)
                    previewTexture = run.previewTexture;
                else
                    previewTexture = GameModes.FirstOrDefault(rn => rn.name.Equals(selectedRun))?.previewTexture;

                Texture2D texture2D = previewTexture ? previewTexture : defaultimage;
                if (texture2D)
                    previewImage.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), Vector2.zero);
            }
            textComponent.text = GameModeNames[gameModeIndex];
        }
    }
}