#if THUNDERKIT_CONFIGURED
using RoR2;
using RoR2.Navigation;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor
{
    [CustomEditor(typeof(SceneInfo), true)]
    public class SceneInfoEditor : UnityEditor.Editor
    {
        //static FieldInfo nodesField = typeof(NodeGraph).GetField("nodes", BindingFlags.NonPublic | BindingFlags.Instance);
        //static FieldInfo linksField = typeof(NodeGraph).GetField("links", BindingFlags.NonPublic | BindingFlags.Instance);

        //private bool pathOperationStarted = false, pathPrepared = false;
        //private Vector3 startNode = Vector3.zero, endNode = Vector3.zero;

        //void OnSceneGUI()
        //{
        //    var so = new SerializedObject(target);
        //    var nodeGraph = (NodeGraph)so.FindProperty("groundNodesAsset").objectReferenceValue;
        //    if (!nodeGraph) return;

        //    var groundNodes = nodesField.GetValue(nodeGraph) as NodeGraph.Node[];
        //    Handles.color = new Color(0, 1, 0, .5f);

        //    foreach (var node in groundNodes)
        //        if (Handles.Button(node.position + Vector3.up * .1f, Quaternion.LookRotation(Vector3.up, Vector3.forward), 2, 2, Handles.ArrowHandleCap))
        //        {
        //            if (!pathOperationStarted)
        //            {
        //                startNode = node.position;
        //                pathOperationStarted = true;
        //                pathPrepared = false;
        //            }
        //            else
        //            {
        //                endNode = node.position;
        //                pathOperationStarted = false;
        //                pathPrepared = true;
        //            }
        //        }


        //    if (pathPrepared)
        //    {
        //        Path path = new Path(nodeGraph);
        //        nodeGraph.ComputePath(new NodeGraph.PathRequest()
        //        {
        //            startPos = startNode,
        //            endPos = endNode,
        //            path = path,
        //            hullClassification = HullClassification.Human
        //        }).Wait();

        //        if (path.status == PathStatus.Valid)
        //        {
        //            for (int index = 1; index < path.waypointsCount; ++index)
        //                Debug.DrawLine(groundNodes[path[index - 1].nodeIndex.nodeIndex].position, groundNodes[path[index].nodeIndex.nodeIndex].position, Color.red, 1f);

        //            pathPrepared = false;
        //        }
        //    }
        //}
    }
}
#endif
