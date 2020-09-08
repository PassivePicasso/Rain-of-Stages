using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RainOfStages.Assets.LocalDevelopment.Scripts
{
    [RequireComponent(typeof(AkAmbient))]
    public class AssignAmbientEvent : MonoBehaviour
    {
        public string EventId;
        // Use this for initialization
        void Awake()
        {
            var wwiseEvent = Resources.Load<WwiseEventReference>($"event/{EventId}");
            var volume = GetComponent<AkAmbient>();
            volume.data.WwiseObjectReference = wwiseEvent;
            Destroy(this);
        }
    }
}