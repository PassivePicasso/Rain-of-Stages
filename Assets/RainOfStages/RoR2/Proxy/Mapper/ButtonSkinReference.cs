﻿#if THUNDERKIT_CONFIGURED
using PassivePicasso.ThunderKit.Proxy.RoR2.UI;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RainOfStages.Behaviours
{
    public class ButtonSkinReference : RoSAssetMapper<ButtonSkinController, RoR2.UI.UISkinData>
    {
        protected override string Field => "skinData";

        protected override BindingFlags FieldBindings => BindingFlags.Public | BindingFlags.Instance;
    }
}
#endif