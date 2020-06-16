using BepInEx.Logging;
using PassivePicasso.RainOfStages.Plugin;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassivePicasso.RainOfStages.UI
{
    public static class UIHelper
    {
        public static ManualLogSource Logger => Plugin.RainOfStages.Instance.RoSLog;

        private static Texture2D defaultPreview = new Texture2D(256, 256);

        public static Action<Image, TMP_Text> OnNext, OnPrevious;

        static UIHelper()
        {
            var w = defaultPreview.width;
            var h = defaultPreview.height;
            defaultPreview.SetPixels(0, 0, w, h, Enumerable.Repeat(Color.black, w * h).ToArray());
        }

        public static void CharacterSelectController_Start(Action<CharacterSelectController> orig, CharacterSelectController self)
        {
            orig(self);
            try
            {
                Logger.LogMessage("Adding Run Selector to Main Menu");

                var campaingselectorbundle = Plugin.RainOfStages.OtherBundles.First(bundle => bundle.name.Equals("campaingselector"));
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

        public static void MainMenuController_Start(Action<MainMenuController> orig, MainMenuController self)
        {
            try
            {
                Logger.LogMessage("Adding Run Selector to Main Menu");


                #region oldCode
                //var singlePlayerButton = GameObject.Find("GenericMenuButton (Singleplayer)");
                //var profileButtonText = singlePlayerButton.GetComponentInChildren<HGTextMeshProUGUI>();
                //var profileButtonImage = singlePlayerButton.GetComponent<Image>();
                //var profileButton = singlePlayerButton.GetComponent<Button>();

                //var profileRectTrans = singlePlayerButton.GetComponent<RectTransform>();
                //var buttonPanelRectTrans = profileRectTrans.parent;

                //var preview = new GameObject("RunImage", typeof(CanvasRenderer));
                //var panel = new GameObject("RunPanel", typeof(CanvasRenderer));
                //var next = new GameObject("NextRun", typeof(CanvasRenderer));
                //var prev = new GameObject("PrevRun", typeof(CanvasRenderer));
                //var header = new GameObject("RunHeader", typeof(CanvasRenderer));

                //var previewRectTrans = preview.AddComponent<RectTransform>();
                //var panelRectTrans = panel.AddComponent<RectTransform>();
                //var nextRectTrans = next.AddComponent<RectTransform>();
                //var prevRectTrans = prev.AddComponent<RectTransform>();
                //var headerRectTrans = header.AddComponent<RectTransform>();

                //var previewImage = preview.AddComponent<Image>();
                //var nextButtonImage = next.AddComponent<Image>();
                //var prevButtonImage = prev.AddComponent<Image>();
                //var panelImage = panel.AddComponent<Image>();

                //CopyImageSettings(profileButtonImage, nextButtonImage);
                //CopyImageSettings(profileButtonImage, prevButtonImage);
                //CopyImageSettings(profileButtonImage, panelImage);

                //var nextButton = next.AddComponent<Button>();
                //var prevButton = prev.AddComponent<Button>();

                //Color buttonNormalColor = profileButton.colors.normalColor;
                //panelImage.color = new Color(buttonNormalColor.r + .1f, buttonNormalColor.g, buttonNormalColor.b, 0.75f);

                //var headerText = header.AddComponent<HGTextMeshProUGUI>();
                //headerText.font = HGTextMeshProUGUI.defaultLanguageFont;
                //headerText.color = profileButtonText.color;
                //headerText.alignment = TMPro.TextAlignmentOptions.Center;
                //headerText.autoSizeTextContainer = profileButtonText.autoSizeTextContainer;
                //headerText.text = "Run Select";

                //prevButton.colors = profileButton.colors;
                //nextButton.colors = profileButton.colors;

                //prevButton.image = prevButtonImage;
                //nextButton.image = nextButtonImage;

                //nextButton.onClick.AddListener(() => OnNext(previewImage));
                //prevButton.onClick.AddListener(() => OnPrevious(previewImage));

                //headerRectTrans.SetParent(panelRectTrans);
                //nextRectTrans.SetParent(panelRectTrans);
                //prevRectTrans.SetParent(panelRectTrans);
                //previewRectTrans.SetParent(panelRectTrans);
                //panelRectTrans.SetParent(buttonPanelRectTrans);
                //panelRectTrans.SetAsFirstSibling();

                //ConfigureTransform(panelRectTrans, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0), new Vector3(0, 5, 0), new Vector2(profileRectTrans.sizeDelta.x, 190));

                //ConfigureTransform(headerRectTrans, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector3(0, -40, 0), new Vector2(profileRectTrans.sizeDelta.x, 40));

                //ConfigureTransform(prevRectTrans, Vector2.zero, new Vector2(0, 1), new Vector2(0, 1), new Vector3(0, -40, 0), new Vector2(30, -42));

                //ConfigureTransform(nextRectTrans, new Vector2(1, 0), Vector2.one, Vector2.one, new Vector3(0, -40, 0), new Vector2(30, -42));

                //ConfigureTransform(previewRectTrans, Vector2.zero, Vector2.one, new Vector2(0, 1), new Vector3(30, -43, 0), new Vector2(-60, -48));

                //UpdateRunPreview(previewImage, defaultPreview);

                //Logger.LogMessage("Finished Adding Run Selector to Main Menu");
                #endregion
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
                orig(self);
            }

        }

        public static void ConfigureTransform(RectTransform transform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector3 anchoredPosition3D, Vector2 sizeDelta)
        {
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.pivot = pivot;
            transform.anchoredPosition3D = anchoredPosition3D;
            transform.sizeDelta = sizeDelta;
        }

        public static void CopyImageSettings(Image from, Image to)
        {
            to.sprite = from.sprite;
            to.type = from.type;
            to.fillCenter = from.fillCenter;
            to.material = from.material;
            to.useSpriteMesh = from.useSpriteMesh;
            to.preserveAspect = from.preserveAspect;
            to.fillAmount = from.fillAmount;
            to.fillOrigin = from.fillOrigin;
            to.fillClockwise = from.fillClockwise;
            to.alphaHitTestMinimumThreshold = from.alphaHitTestMinimumThreshold;
        }
    }
}