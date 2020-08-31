#if THUNDERKIT_CONFIGURED
using PassivePicasso.RainOfStages.Proxy;
using PassivePicasso.ThunderKit.Proxy.RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PassivePicasso.ThunderKit.Proxy
{
    public class MusicTrackDefRef : global::RoR2.MusicTrackDef, IProxyReference<global::RoR2.MusicTrackDef>
    {
        void Awake()
        {
            if (Application.isEditor) return;
            var trackDef = (global::RoR2.MusicTrackDef)ResolveProxy();
            this.catalogIndex = trackDef.catalogIndex;
            this.comment = trackDef.comment;
            this.soundBank = trackDef.soundBank;
            this.states = trackDef.states;
        }

        public global::RoR2.MusicTrackDef ResolveProxy() => LoadTrack<global::RoR2.MusicTrackDef>();

        private T LoadTrack<T>() where T : global::RoR2.MusicTrackDef
        {
            string name = (this as ScriptableObject)?.name;
            var card = Resources.Load<T>($"musictrackdefs/{name}");
            return card;
        }
    }
}
#endif