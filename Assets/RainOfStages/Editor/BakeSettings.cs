using System;
using UnityEngine;
using UnityEngine.AI;

namespace PassivePicasso.RainOfStages
{
    public class BakeSettings : ScriptableObject
    {
        public bool DebugMode;
        public float minRegionArea;
        public int tileSize;
        public Vector3 globalNavigationOffset;
        public float AirNodeSize;
        public float MaximumSurfaceDistance;
        public float MinimumSurfaceDistance;
        public NavMeshBuildSettings bakeSettings => new NavMeshBuildSettings
        {
            minRegionArea = minRegionArea,
            overrideTileSize = true,
            tileSize = tileSize
        };
    }
}