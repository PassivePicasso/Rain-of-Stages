using BepInEx.Logging;
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

    public class ConfigureCampaignPanel : NetworkBehaviour
    {
        static readonly Regex gameModeCheck = new Regex(".*gamemode\\s(\\w+);.*", RegexOptions.Compiled | RegexOptions.Singleline);
        static ManualLogSource Logger => RainOfStages.Instance.RoSLog;

        private static ConfigureCampaignPanel instance;

        [SyncVar]
        private int selectedRunIndex;

        public Button nextButton;
        public Button previousButton;
        public Image previewImage;
        public TMP_Text campaignTitle;

        public Texture2D defaultimage;

        IReadOnlyList<string> RunNames => RainOfStages.Instance.RunNames;
        IReadOnlyList<Run> Runs => RainOfStages.Instance.Runs;


        // Start is called before the first frame update
        void OnEnable()
        {
            nextButton.onClick.AddListener(CmdNextRun);
            previousButton.onClick.AddListener(CmdPreviousRun);
            UpdateRunSelection(previewImage, campaignTitle);

            ConfigureCampaignPanel.instance = this;
        }

        private void OnDisable()
        {
            nextButton.onClick.RemoveAllListeners();
            previousButton.onClick.RemoveAllListeners();

            ConfigureCampaignPanel.instance = null;
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
                var index = instance.RunNames.ToList().IndexOf(runName);
                if (index != instance.selectedRunIndex)
                    instance.selectedRunIndex = index;

                if (instance.selectedRunIndex == -1) instance.selectedRunIndex = 0;
                if (NetworkServer.active)
                {
                    PreGameController.instance.NetworkgameModeIndex = instance.selectedRunIndex;
                }

                instance.UpdateRunSelection(instance.previewImage, instance.campaignTitle);
            }
        }

        //[Hook("RoR2.PreGameController+GameModeConVar")]
        //public void SetString(string newValue)
        //{
        //    if (NetworkServer.active)
        //    {
        //    }
        //}

        [Command]
        void CmdNextRun()
        {
            if (++selectedRunIndex >= RunNames.Count) selectedRunIndex = 0;
            Send($"gamemode {RunNames[selectedRunIndex]};");
        }


        [Command]
        void CmdPreviousRun()
        {
            if (--selectedRunIndex < 0) selectedRunIndex = RunNames.Count - 1;
            Send($"gamemode {RunNames[selectedRunIndex]};");
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
            string selectedRun = RunNames[selectedRunIndex];
            Logger.LogInfo($"Selected Run: {selectedRun}");
            var run = GameModeCatalog.FindGameModePrefabComponent(selectedRun) as Run;
            if (previewImage)
            {
                previewImage.preserveAspect = true;
                Texture2D previewTexture = null;
                if (run && run.previewTexture)
                    previewTexture = run.previewTexture;
                else
                    previewTexture = Runs.FirstOrDefault(rn => rn.name.Equals(selectedRun))?.previewTexture;

                Texture2D texture2D = previewTexture ? previewTexture : defaultimage;
                if (texture2D)
                    previewImage.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), Vector2.zero);
            }
            textComponent.text = RunNames[selectedRunIndex];
        }
    }
}