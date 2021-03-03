using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor.NodeGraph
{
    public struct VoxelCheckJob : IJobParallelFor
    {
        public static readonly Vector3 Vec3MaxValue = Vector3.one * float.MaxValue;

        [ReadOnly]
        public NativeArray<Vector3> Origins;
        [ReadOnly]
        public NativeArray<Vector3> Directions;

        public NativeArray<RaycastCommand> Commands;

        public float MaximumDistance;
        public int LayerMask;
        public void Execute(int index)
        {
            var directionIndex = index % Directions.Length;
            var positionIndex = index % Origins.Length;
            var position = Origins[positionIndex];

            Commands[index] = new RaycastCommand { from = position, distance = MaximumDistance, layerMask = LayerMask, direction = Directions[directionIndex], maxHits = 1 };
        }
    }
}