using System.Linq;
using UnityEngine;
using VoxelSystem;

namespace PassivePicasso.RainOfStages.Shared
{
    [ExecuteInEditMode]
    public class Voxelizer : MonoBehaviour
    {
        public ComputeShader VoxelizerShader;
        public ComputeShader TextureSlicer;
        public Texture2D ColorMap;
        public Transform World;
        public bool update;

        public int resolution;
        public bool volume, pow2, showEmpty;

        public Texture3D TexturePreview;

        private Voxel_t[] voxels;
        //private List<Voxel_t> voxelList;
        private GPUVoxelData gpuVoxelData;

        // Update is called once per frame
        void Update()
        {
            if (!update) return;
            update = false;
            var meshFilters = World.GetComponentsInChildren<MeshFilter>().ToArray();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                i++;
            }
            var mesh = new Mesh();
            mesh.CombineMeshes(combine);

            gpuVoxelData?.Dispose();

            gpuVoxelData = GPUVoxelizer.Voxelize(VoxelizerShader, mesh, resolution, volume, pow2);
            var volumeTexture = GPUVoxelizer.BuildTexture3D(VoxelizerShader, gpuVoxelData, RenderTextureFormat.ARGBFloat, FilterMode.Bilinear);
            GPUVoxelizer.BuildBetterVoxels(VoxelizerShader, gpuVoxelData);

            voxels = gpuVoxelData.GetData();
            var emptyCount = voxels.Count(v => v.IsEmpty());
            Debug.Log($"Found {emptyCount} empty voxels");
            Debug.Log($"Found {voxels.Length - emptyCount} non-empty voxels");

            Mesh results;
            if (showEmpty)
                results = VoxelMesh.BuildEmpty(gpuVoxelData.GetData(), gpuVoxelData.UnitLength);
            else
                results = VoxelMesh.Build(gpuVoxelData.GetData(), gpuVoxelData.UnitLength);

            results.name = "VoxelizedWorld";
            GetComponent<MeshFilter>().mesh = results;
        }

    }
}