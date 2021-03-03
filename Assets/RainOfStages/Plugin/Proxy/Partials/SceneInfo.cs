using global::RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.ThunderKit.Proxy
{

    public partial class SceneInfo : global::RoR2.SceneInfo
    {
        static FieldInfo nodesField = typeof(NodeGraph).GetField("nodes", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo linksField = typeof(NodeGraph).GetField("links", BindingFlags.NonPublic | BindingFlags.Instance);

        [SerializeField]
        public bool DebugNoCeiling;
        [SerializeField]
        public bool DebugTeleporterOk;
        [SerializeField]
        public bool DebugLinks;
        [SerializeField]
        public bool DebugNodes;
        [SerializeField]
        public bool DebugAirLinks;
        [SerializeField]
        public bool DebugAirNodes;
        public float AirNodeSize;
        private float nodeMeshSize;

        public Color HumanColor = Color.white;
        public Color GolemColor = Color.white;
        public Color QueenColor = Color.white;
        public Color NoCeilingColor = Color.green;
        public Color TeleporterOkColor = Color.yellow;
        public Material NodeMaterial;
        private HullMask[] masks = new[] { HullMask.Human, HullMask.Golem, HullMask.BeetleQueen };
        private Dictionary<HullMask, Color> colormap;


#if UNITY_EDITOR

        private Mesh mesh;

        private static Mesh NewCube(float size = 1)
        {
            var _cubeMesh = new Mesh()
            {
                vertices = new[]{
                    new Vector3 (0, 0, 0),
                    new Vector3 (1, 0, 0) * size,
                    new Vector3 (1, 1, 0) * size,
                    new Vector3 (0, 1, 0) * size,
                    new Vector3 (0, 1, 1) * size,
                    new Vector3 (1, 1, 1) * size,
                    new Vector3 (1, 0, 1) * size,
                    new Vector3 (0, 0, 1) * size },
                triangles = new[] { 0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 1, 5, 6, 0, 7, 4, 0, 4, 3, 5, 4, 7, 5, 7, 6, 0, 6, 7, 0, 1, 6 }
            };
            _cubeMesh.RecalculateNormals();
            return _cubeMesh;
        }

        private void OnDrawGizmosSelected()
        {
            if (colormap == null)
                colormap = new Dictionary<HullMask, Color> {
                    { HullMask.Human, HumanColor },
                    { HullMask.Golem, GolemColor },
                    { HullMask.BeetleQueen, QueenColor },
                };
            else
            {
                colormap[HullMask.Human] = HumanColor;
                colormap[HullMask.Golem] = GolemColor;
                colormap[HullMask.BeetleQueen] = QueenColor;
            }
            var so = new SerializedObject(this);
            var airNodeGraph = (NodeGraph)so.FindProperty("airNodesAsset").objectReferenceValue;
            if (airNodeGraph)
            {
                var airLinks = linksField.GetValue(airNodeGraph) as NodeGraph.Link[];
                var airNodes = nodesField.GetValue(airNodeGraph) as NodeGraph.Node[];

                if (DebugAirNodes)
                {
                    if (mesh == null || nodeMeshSize != AirNodeSize) mesh = NewCube(nodeMeshSize = AirNodeSize);

                    List<Matrix4x4> transformList = new List<Matrix4x4>();

                    foreach (var node in airNodes)
                    {
                        var position = node.position;
                        Matrix4x4 matrix = new Matrix4x4();
                        matrix.SetTRS(position, Quaternion.Euler(Vector3.zero), Vector3.one);
                        transformList.Add(matrix);
                        //foreach (var mask in masks)
                        //    if (!node.forbiddenHulls.HasFlag(mask))
                        //    {
                        //        //Gizmos.color = colormap[mask];
                        //        //Gizmos.DrawCube(position, Vector3.one * AirNodeSize);
                        //        Graphics.DrawMesh(mesh, position, Quaternion.identity, NodeMaterial, 0);
                        //        position += Vector3.up;
                        //    }

                    }

                    IEnumerable<Matrix4x4> Batch(int page) => transformList.Skip(page * 1000).Take(1000);
                    for (int i = 0; Batch(i).Any(); i++)
                        Graphics.DrawMeshInstanced(mesh, 0, NodeMaterial, Batch(i).ToList());
                }


                if (DebugAirLinks)
                {
                    foreach (var link in airLinks)
                    {
                        Gizmos.color = HumanColor;
                        if (((HullMask)link.hullMask).HasFlag(HullMask.Golem)) Gizmos.color = GolemColor;
                        if (((HullMask)link.hullMask).HasFlag(HullMask.BeetleQueen)) Gizmos.color = QueenColor;

                        Vector3 nodeAPos = airNodes[link.nodeIndexA.nodeIndex].position;
                        Vector3 nodeBPos = airNodes[link.nodeIndexB.nodeIndex].position;
                        Gizmos.DrawLine(nodeAPos, nodeBPos);
                    }
                }
            }


            var groundNodeGraph = (NodeGraph)so.FindProperty("groundNodesAsset").objectReferenceValue;
            if (groundNodeGraph)
            {
                var groundNodes = nodesField.GetValue(groundNodeGraph) as NodeGraph.Node[];
                var groundLinks = linksField.GetValue(groundNodeGraph) as NodeGraph.Link[];

                if (DebugLinks)
                {
                    foreach (var link in groundLinks)
                    {
                        Gizmos.color = HumanColor;
                        if (((HullMask)link.hullMask).HasFlag(HullMask.Golem)) Gizmos.color = GolemColor;
                        if (((HullMask)link.hullMask).HasFlag(HullMask.BeetleQueen)) Gizmos.color = QueenColor;

                        Vector3 nodeAPos = groundNodes[link.nodeIndexA.nodeIndex].position;
                        Vector3 nodeBPos = groundNodes[link.nodeIndexB.nodeIndex].position;
                        var displacement = nodeBPos - nodeAPos;
                        var direction = displacement.normalized;
                        Gizmos.DrawLine(nodeAPos, nodeBPos);
                        Gizmos.DrawLine(nodeAPos + Vector3.up * 2, nodeAPos + (displacement * 0.2f));

                    }
                }


                foreach (var node in groundNodes)
                {
                    if (DebugNoCeiling && node.flags.HasFlag(NodeFlags.NoCeiling))
                    {
                        Gizmos.color = NoCeilingColor;
                        Gizmos.DrawLine(node.position, node.position + (Vector3.up * 10f));
                    }
                    if (DebugTeleporterOk && node.flags.HasFlag(NodeFlags.TeleporterOK))
                    {
                        Gizmos.color = TeleporterOkColor;
                        float radius = 2;
                        Vector3 offsetPosition = node.position + (Vector3.up * radius);

                        Gizmos.DrawSphere(offsetPosition, radius);
                    }
                    if (DebugNodes)
                    {
                        var position = node.position + Vector3.up * 0.5f;
                        foreach (var mask in masks)
                            if (!node.forbiddenHulls.HasFlag(mask))
                            {
                                Gizmos.color = colormap[mask];
                                Gizmos.DrawCube(position, Vector3.one);
                                position += Vector3.up;
                            }
                    }
                }

            }


        }

#endif

    }
}
