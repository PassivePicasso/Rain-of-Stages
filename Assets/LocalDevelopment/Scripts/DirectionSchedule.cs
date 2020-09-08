using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PassivePicasso.RainOfStages.Variants.Behaviors
{
    public class DirectionSchedule : MonoBehaviour
    {
        public enum SchedulerState { Holding, Transitioning }

        public float holdTime;
        public float transitionTime;
        public Vector3[] Directions;

        private float elapsedTime;
        private int directionIndex;
        private SchedulerState state;

        // Update is called once per frame
        void Update()
        {
            switch (state)
            {
                case SchedulerState.Holding:
                    if (elapsedTime < holdTime) elapsedTime += Time.deltaTime;
                    else
                    {
                        state = SchedulerState.Transitioning;
                        elapsedTime = 0;
                    }
                    break;
                case SchedulerState.Transitioning:
                    if (elapsedTime > transitionTime)
                    {
                        state = SchedulerState.Holding;
                        elapsedTime = 0;
                        directionIndex++;
                    }
                    else
                    {
                        elapsedTime += Time.deltaTime;

                        var curDir = transform.forward;
                        var nextDirI = directionIndex % Directions.Length;
                        var nextDir = Directions[nextDirI];
                        var angle = Vector3.Angle(curDir, nextDir);
                        var rads = Mathf.Deg2Rad * angle;
                        var radiansPerSecond = (rads / transitionTime) * Time.deltaTime;
                        transform.forward = Vector3.RotateTowards(curDir, nextDir, radiansPerSecond, 1);
                    }
                    break;
            }
        }
    }
}
