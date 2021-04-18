using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThunderKit.Core.Editor.Windows;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using VoxelSystem;
using static RoR2.Navigation.NodeGraph;
using Debug = UnityEngine.Debug;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018_1_OR_NEWER
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif



namespace PassivePicasso.RainOfStages
{
    using static Extensions;
    using static ThunderKit.Core.UIElements.TemplateHelpers;

    public class NodeGraphBaker : TemplatedWindow
    {
        public static System.Reflection.FieldInfo NodesField = typeof(NodeGraph).GetField("nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static System.Reflection.FieldInfo LinksField = typeof(NodeGraph).GetField("links", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static Stopwatch watcher = new Stopwatch();

        public ComputeShader VoxelizerShader;
        public bool DebugMode = false;
        public float minRegionArea = 100;
        public int tileSize = 32;
        public Vector3 globalNavigationOffset = new Vector3(0, 0.1f, 0);
        public int SimplifySteps = 1;
        public int NeighborRemapThreshold = 20;
        public float Resolution = 50;
        public float MaximumSurfaceDistance = 30;
        public float MinimumSurfaceDistance = 0.5f;
        public NavMeshBuildSettings settings => new NavMeshBuildSettings
        {
            minRegionArea = minRegionArea,
            overrideTileSize = true,
            tileSize = tileSize
        };



        [MenuItem("Tools/Rain of Stages/Navigation Baking")]
        public static void OpenBaker()
        {
            var baker = GetWindow<NodeGraphBaker>();
            baker.titleContent = new GUIContent("RoS Navigation");
            baker.DebugMode = false;
            baker.minRegionArea = 100;
            baker.tileSize = 16;
            baker.globalNavigationOffset = new Vector3(0, 0.1f, 0);
            baker.Resolution = 50;
            baker.MaximumSurfaceDistance = 30;
            baker.MinimumSurfaceDistance = 0.5f;

            var potentialShaders = AssetDatabase.FindAssets("t:ComputeShader Voxelizer");
            var shaderPaths = potentialShaders.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var theShadersIwant = shaderPaths.Where(path => path.Contains("RainOfStages/Editor/Shaders")).ToArray();
            var shader = shaderPaths.Select(AssetDatabase.LoadAssetAtPath<ComputeShader>)
                                        .First();
            baker.VoxelizerShader = shader;
            baker.Show();
        }

        public override void OnEnable()
        {
            rootVisualElement.Clear();
            GetTemplateInstance(GetType().Name, rootVisualElement, path => path.StartsWith("Packages/twiner-rainofstages") || path.StartsWith("Assets/RainOfStages"));
            titleContent = new GUIContent(ObjectNames.NicifyVariableName(GetType().Name), ThunderKitIcon);
            rootVisualElement.Bind(new SerializedObject(this));

            var bakeButton = rootVisualElement.Q<Button>();
            var timeValue = rootVisualElement.Q<Label>("bake-time");
            bakeButton.clickable.clicked += Clickable_clicked;
            void Clickable_clicked()
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                Build();
                watch.Stop();
                timeValue.text = $"{watch.Elapsed:mm\\:ss\\:fff}";
                Debug.Log($"Bake took: {watch.Elapsed.TotalMilliseconds}ms");
            }
        }
        public void Build()
        {
            if (DebugMode)
            {
                watcher = new Stopwatch();
                watcher.Start();
            }

            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var sceneInfo = rootObjects.FirstOrDefault(ro => ro.GetComponent<SceneInfo>() != null).GetComponent<SceneInfo>();

            Undo.RecordObject(sceneInfo, "Bake NodeGraph");
            var worlds = rootObjects.Count(ro => ro.layer == LayerMask.NameToLayer("World"));
            if (worlds > 1)
            {
                Debug.LogError("Bake Failed: Multiple root GameObjects found on World Layer");
                return;
            }

            var world = rootObjects.FirstOrDefault(ro => ro.layer == LayerMask.NameToLayer("World"));

            if (world == null)
            {
                Debug.LogError("Bake Failed: Root GameObject on layer World not found");
                return;
            }

            Log("Initialization");

            var meshFilters = world.GetComponentsInChildren<MeshFilter>().Where(mf => mf.gameObject.layer == LayerMask.NameToLayer("World")).ToArray();
            var meshes = meshFilters.Select(mf => mf.sharedMesh);

            var sources = meshFilters.Select(mf => new NavMeshBuildSource
            {
                area = 0,
                component = mf.gameObject.GetComponent<MeshRenderer>(),
                shape = NavMeshBuildSourceShape.Mesh,
                size = mf.sharedMesh.bounds.size,
                sourceObject = mf.sharedMesh,
                transform = mf.transform.localToWorldMatrix
            }).ToList();

            Log("Source Collection");

            Bounds b = meshes.First().bounds;
            foreach (var mesh in meshes.Skip(1))
                b.Encapsulate(mesh.bounds);

            Log("Bounds encapsulation");

            var airNodeGraph = BakeVoxelAirNodes(world.transform, sources);
            Log("Saved Air Graph");

            var groundNodeGraph = BakeGroundNodes(world.transform, b.size.y * 2, sources);
            Log("Saved Ground Graph");

            var so = new SerializedObject(sceneInfo);
            so.FindProperty("groundNodesAsset").objectReferenceValue = groundNodeGraph;
            so.FindProperty("airNodesAsset").objectReferenceValue = airNodeGraph;
            so.ApplyModifiedProperties();

            Log("Reference Graph");

            if (DebugMode)
            {
                watcher.Reset();
                watcher.Stop();
            }
        }

        NodeGraph BakeVoxelAirNodes(Transform world, List<NavMeshBuildSource> sources)
        {
            var colliders = new Collider[128];
            var mesh = new Mesh();
            var nodeMap = new Dictionary<uint3, (Voxel_t, Node)>();
            var meshFilters = world.GetComponentsInChildren<MeshFilter>().Where(mf => mf.gameObject.layer == LayerIndex.world.intVal).ToArray();
            Log("Mesh location");

            var combine = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            mesh.CombineMeshes(combine);
            mesh.RecalculateBounds();
            Log("Mesh combination");

            var gpuVoxelData = GPUVoxelizer.Voxelize(VoxelizerShader, mesh, (int)Resolution, true);
            GPUVoxelizer.BuildBetterVoxels(VoxelizerShader, gpuVoxelData);
            var voxels = gpuVoxelData.GetData();
            Log("Voxelization");
            DestroyImmediate(mesh);
            CollectNodes(colliders, nodeMap, voxels);

            var nodeNeighbors = new Dictionary<uint3, List<uint3>>();
            var removedIds = new List<uint3>();
            var remapIds = new Dictionary<uint3, uint3>();
            (Voxel_t voxel, Node node)[] pairs = nodeMap.Values.ToArray();
            foreach (var pair in pairs)
            {
                var id = pair.voxel.id;
                var node = pair.node;
                var range = 1;
                nodeNeighbors[id] = new List<uint3>();

                for (var x = -range; x <= range; x++)
                    for (var y = -range; y <= range; y++)
                        for (var z = -range; z <= range; z++)
                            if (x == 0 && y == 0 && z == 0) continue;
                            else
                            {
                                var neighborX = id.x + x;
                                var neighborY = id.y + y;
                                var neighborZ = id.z + z;
                                var neighborId = new uint3((uint)neighborX, (uint)neighborY, (uint)neighborZ);

                                if (nodeMap.ContainsKey(neighborId)) nodeNeighbors[id].Add(neighborId);
                            }

                if (nodeNeighbors[id].Count <= 2)
                {
                    nodeMap.Remove(id);
                    nodeNeighbors.Remove(id);
                    removedIds.Add(id);
                }
            }

            //for (int reduce = 0; reduce < SimplifySteps; reduce++)
            foreach (var (voxel, node) in pairs)
            {
                if (!nodeNeighbors.ContainsKey(voxel.id)) continue;
                if (remapIds.ContainsKey(voxel.id)) continue;
                if (nodeNeighbors[voxel.id].Count > NeighborRemapThreshold)
                {
                    foreach (var neighborId in nodeNeighbors[voxel.id])
                    {
                        remapIds[neighborId] = voxel.id;
                        nodeMap.Remove(neighborId);
                        nodeNeighbors.Remove(neighborId);
                    }
                }
            }
            Log("Node neighborization");

            foreach (var map in nodeNeighbors)
            {
                map.Value.RemoveAll(x=> removedIds.Contains(x));
                for (int i = 0; i < map.Value.Count; i++)
                    map.Value[i] = remapIds.ContainsKey(map.Value[i]) ? remapIds[map.Value[i]] : map.Value[i];
            }

            var links = new List<Link>();
            pairs = nodeMap.Values.ToArray();
            var lookup = pairs.Select((map, i) => (i, map.voxel.id)).ToDictionary(m => m.id, m => m.i);
            var nodes = pairs.Select(pair => pair.node).ToArray();

            var linkIndex = 0;
            for (int i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i];
                var id = pair.voxel.id;
                var node = nodes[i];
                var neighbors = nodeNeighbors[id];

                var count = neighbors.Count;
                if (count == 0)
                    Debug.Log("Added zero size link list");

                node.linkListIndex = new LinkListIndex
                {
                    index = linkIndex,
                    size = (uint)count
                };

                foreach (var neighbor in neighbors)
                    links.Add(ComputeLink(i, lookup[neighbor], node, nodes[lookup[neighbor]]));

                linkIndex += count;

                nodes[i] = node;
            }

            Log($"Indexing {nodeMap.Count} voxels");
            gpuVoxelData?.Dispose();

            return SaveGraph($"{SceneManager.GetActiveScene().name}_AirNodeGraph", nodes, links.ToArray());
        }

        void OnSceneGUI()
        {
            if (Event.current.type == EventType.MouseMove)
                SceneView.currentDrawingSceneView.Repaint();
        }

        private void CollectNodes(Collider[] colliders, Dictionary<uint3, (Voxel_t, Node)> nodeMap, Voxel_t[] voxels)
        {
            var navMeshObjects = GameObject.FindObjectsOfType<NavMeshObstacle>();
            var validNMOs = navMeshObjects.Where(nmo => nmo.isActiveAndEnabled && nmo.carving && nmo.shape == NavMeshObstacleShape.Box).ToArray();
            var outOfBounds = validNMOs.Select(nmo =>
            {
                var transform = nmo.transform;
                var size = new Vector3(nmo.transform.lossyScale.x * nmo.size.x,
                                       nmo.transform.lossyScale.y * nmo.size.y,
                                       nmo.transform.lossyScale.z * nmo.size.z);
                var bounds = new Bounds(nmo.transform.position + nmo.center, size);
                return (nmo.transform, bounds);
            }).ToList();

            var directions = new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.down, Vector3.up };

            foreach (var voxel in voxels)
                if (voxel.IsEmpty())
                {
                    //navmesh carving check
                    if (outOfBounds.Any(set => set.bounds.Contains(set.transform.InverseTransformDirection(voxel.position)))) continue;

                    bool skip = false;
                    bool hit = false;
                    //Backface check
                    for (int i = 0; i < directions.Length; i++)
                        if (Physics.Raycast(voxel.position, directions[i], out RaycastHit hitInfo, MaximumSurfaceDistance, LayerIndex.world.mask))
                        {
                            hit = true;
                            if (Physics.Raycast(hitInfo.point, directions[i] * -1, out RaycastHit nextHitInfo, MaximumSurfaceDistance, LayerIndex.world.mask)
                             && nextHitInfo.distance < hitInfo.distance)
                            {
                                skip = true;
                                break;
                            }
                        }

                    if (!hit || skip) continue;

                    //no ceiling check
                    var upHit = Physics.Raycast(new Ray(voxel.position, Vector3.up), out RaycastHit upHitInfo, MaximumSurfaceDistance, LayerIndex.world.mask);

                    var overlaps = Physics.OverlapBoxNonAlloc(voxel.position, Vector3.one * MinimumSurfaceDistance, colliders, Quaternion.identity, LayerIndex.world.mask);
                    if (overlaps > 0) continue;

                    var withinDistance = Physics.OverlapBoxNonAlloc(voxel.position, Vector3.one * MaximumSurfaceDistance, colliders, Quaternion.identity, LayerIndex.world.mask);
                    if (withinDistance <= 0) continue;

                    bool queenFit = Physics.OverlapBoxNonAlloc(voxel.position, Vector3.one * 10f, colliders, Quaternion.identity, LayerIndex.world.mask) <= 0;
                    bool golemFit = queenFit ? queenFit : Physics.OverlapBoxNonAlloc(voxel.position, Vector3.one * 4, colliders, Quaternion.identity, LayerIndex.world.mask) <= 0;

                    var node = new Node
                    {
                        position = voxel.position,
                        forbiddenHulls = (queenFit ? HullMask.None : HullMask.BeetleQueen) | (golemFit ? HullMask.None : HullMask.Golem),
                        flags = NodeFlags.NoShrineSpawn | NodeFlags.NoChestSpawn
                    };

                    node.flags |= !upHit ? NodeFlags.NoCeiling : NodeFlags.None;
                    nodeMap.Add(voxel.id, (voxel, node));
                }
            Log("Node construction");
        }

        NodeGraph BakeGroundNodes(Transform world, float worldheight, List<NavMeshBuildSource> sources)
        {
            var humanSettings = HullSettings(settings, 0.5f, 2, 40, .5f);
            var golemSettings = HullSettings(settings, 4, 5, 40, .5f);
            var queenSettings = HullSettings(settings, 10, 20, 40, .5f);
            watcher.Reset();
            watcher.Start();

            var humanTri = GenerateTriangulation(world, humanSettings, sources);
            var golemTri = GenerateTriangulation(world, golemSettings, sources);
            var queenTri = GenerateTriangulation(world, queenSettings, sources);

            Log("Triangulation");

            humanTri.vertices = humanTri.vertices.Select(v => v + globalNavigationOffset).ToArray();
            golemTri.vertices = golemTri.vertices.Select(v => v + globalNavigationOffset).ToArray();
            queenTri.vertices = queenTri.vertices.Select(v => v + globalNavigationOffset).ToArray();

            Log("Triangulation offset");

            var queenProjection = new NavProjection { vertices = queenTri.vertices, indices = queenTri.indices };
            var golemProjection = new NavProjection { vertices = golemTri.vertices, indices = golemTri.indices };
            var humanProjection = new NavProjection { vertices = humanTri.vertices, indices = humanTri.indices };

            MeshWelder.Weld(ref queenProjection.vertices, ref queenProjection.indices);
            MeshWelder.Weld(ref golemProjection.vertices, ref golemProjection.indices);
            MeshWelder.Weld(ref humanProjection.vertices, ref humanProjection.indices);

            Log("Decimate NavMesh");

            var nodes = new Node[humanProjection.vertices.Length];

            for (int i = 0; i < humanProjection.vertices.Length; i++) humanProjection.lookup[i] = new List<triangle>();
            for (int i = 0; i < golemProjection.vertices.Length; i++) golemProjection.lookup[i] = new List<triangle>();
            for (int i = 0; i < queenProjection.vertices.Length; i++) queenProjection.lookup[i] = new List<triangle>();
            Log("Initialize Lookup tables");

            ConstructTriangles(humanProjection);
            ConstructTriangles(golemProjection);
            ConstructTriangles(queenProjection);
            Log("Triangle Construction");

            NeighborizeTriangles(humanProjection);
            NeighborizeTriangles(golemProjection);
            NeighborizeTriangles(queenProjection);
            Log("Triangle Neighborization");

            IEnumerable<Link> GetLinks(int currNodeIndex)
            {
                foreach (var triangle in humanProjection.lookup[currNodeIndex])
                {
                    var (oppEdgeIndexA, oppEdgeIndexB) = triangle.OppositeEdge(currNodeIndex);
                    yield return ComputeLink(currNodeIndex, oppEdgeIndexA, nodes[currNodeIndex], nodes[oppEdgeIndexA]);
                    yield return ComputeLink(currNodeIndex, oppEdgeIndexB, nodes[currNodeIndex], nodes[oppEdgeIndexB]);

                    var oppositeIndex = triangle.OppositeIndex(currNodeIndex);
                    if (oppositeIndex > -1)
                    {
                        yield return ComputeLink(currNodeIndex, oppositeIndex, nodes[currNodeIndex], nodes[oppositeIndex]);
                    }
                }
            }
            List<IEnumerable<Link>> prepareLinks = new List<IEnumerable<Link>>();
            for (int i = 0; i < nodes.Length; i++)
            {
                var flags = NodeFlags.None;
                if (!Physics.Raycast(new Ray(humanProjection.vertices[i], Vector3.up), worldheight, (int)LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    flags |= NodeFlags.NoCeiling;

                if (TestTeleporterOK(humanProjection.vertices[i]))
                    flags |= NodeFlags.TeleporterOK;

                nodes[i] = new Node
                {
                    position = humanProjection.vertices[i],
                    flags = flags,
                    forbiddenHulls = HullMask.Golem | HullMask.BeetleQueen,
                };

                if (golemProjection.Contains(nodes[i]))
                    nodes[i].forbiddenHulls ^= HullMask.Golem;

                if (queenProjection.Contains(nodes[i]))
                    nodes[i].forbiddenHulls ^= HullMask.BeetleQueen;

                prepareLinks.Add(GetLinks(i));
            }
            Log("Node Construction");
            Log($"Preparing Link Index");

            var linksByIndex = prepareLinks.Select(enumerable => enumerable.ToArray()).ToArray();
            Log($"Link Indexing: Created {linksByIndex.Length} link sets");

            var links = linksByIndex.SelectMany(l => l).ToArray();
            Log($"Collected {links.Length} links");

            var linkIndex = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                int nodeIndex = i;
                int size = linksByIndex[i].Length;
                var node = nodes[nodeIndex];
                node.linkListIndex = new LinkListIndex
                {
                    index = linkIndex,
                    size = (uint)size
                };
                if (size == 0)
                    Debug.Log("Added zero size link list");
                linkIndex += size;
                nodes[nodeIndex] = node;
            }
            Log("Node Link Cross referencing");

            return SaveGraph($"{SceneManager.GetActiveScene().name}_GroundNodeGraph", nodes, links);
        }

        static NavMeshBuildSettings HullSettings(NavMeshBuildSettings buildSettings, float agentRadius, float agentHeight, float slope, float climb)
        {
            return new NavMeshBuildSettings
            {
                agentClimb = climb,
                agentHeight = agentHeight,
                agentRadius = agentRadius,
                agentSlope = slope,
                agentTypeID = buildSettings.agentTypeID,
                debug = buildSettings.debug,
                minRegionArea = buildSettings.minRegionArea,
                overrideTileSize = buildSettings.overrideTileSize,
                overrideVoxelSize = true,
                tileSize = buildSettings.tileSize,
                voxelSize = agentRadius / 2f,
            };
        }

        static Link ComputeLink(int aIndex, int bIndex, Node a, Node b)
        {
            var mask = HullMask.BeetleQueen | HullMask.Golem | HullMask.Human;
            mask ^= a.forbiddenHulls | b.forbiddenHulls;

            var jumpheight = (b.position.y - a.position.y);
            jumpheight = jumpheight > 0 ? jumpheight * 1.25f : jumpheight * -1.25f;

            Link link = new Link
            {
                nodeIndexA = new NodeIndex { nodeIndex = aIndex },
                nodeIndexB = new NodeIndex { nodeIndex = bIndex },
                distanceScore = Mathf.Sqrt((b.position - a.position).sqrMagnitude),
                minJumpHeight = 0,//jumpheight,
                hullMask = (int)mask,
                jumpHullMask = (int)mask,
                gateIndex = 0,
                maxSlope = 0
            };
            return link;
        }

        private static void NeighborizeTriangles(NavProjection projection)
        {
            foreach (var triangle in projection.triangles)
            {
                var potentials = projection.lookup[triangle.IndexA].Union(projection.lookup[triangle.IndexB]).Union(projection.lookup[triangle.IndexC]);
                foreach (var potential in potentials)
                    triangle.AssignNeighbor(potential);
            }
        }

        private static void ConstructTriangles(NavProjection projection)
        {
            for (int i = 0; i < projection.indices.Length; i += 3)
            {
                var tri = new triangle
                {
                    IndexA = projection.indices[i + 0],
                    IndexB = projection.indices[i + 1],
                    IndexC = projection.indices[i + 2],
                    Plane = new Plane(projection.vertices[projection.indices[i + 0]],
                                      projection.vertices[projection.indices[i + 1]],
                                      projection.vertices[projection.indices[i + 2]])
                };
                projection.lookup[tri.IndexA].Add(tri);
                projection.lookup[tri.IndexB].Add(tri);
                projection.lookup[tri.IndexC].Add(tri);
                projection.triangles.Add(tri);
            }
        }

        public bool TestTeleporterOK(Vector3 position)
        {
            float radius = 15f;
            int steps = 20;
            float height = 7f;
            float degrees = 360f / (float)steps;
            for (int index = 0; index < steps; ++index)
            {
                RaycastHit hitInfo;
                if (!Physics.Raycast(new Ray(position + Quaternion.AngleAxis(degrees * (float)index, Vector3.up) * (Vector3.forward * radius) + Vector3.up * height, Vector3.down), out hitInfo, height + 3f, (int)LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    return false;
            }
            Debug.DrawRay(position, Vector3.up * 20f, Color.green, 15f);
            return true;
        }


        /// <summary>
        /// This may be totally wrong...
        /// </summary>
        /// <param name="triangle"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        internal float Area(triangle triangle, Node[] nodes)
        {
            var aNode = nodes[triangle.IndexA];
            var bNode = nodes[triangle.IndexA];
            var cNode = nodes[triangle.IndexA];
            var aPos = aNode.position;
            var bPos = bNode.position;
            var cPos = cNode.position;

            var vab = aPos - bPos;
            var vac = aPos - cPos;

            var height = Vector3.Cross(vab, vac).magnitude / vab.magnitude;
            var area = (vab.magnitude * height) / 2;
            return area;
        }

        NavMeshTriangulation GenerateTriangulation(Transform world, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources = null)
        {
            Stopwatch watcher = new Stopwatch();
            watcher.Start();
            NavMesh.RemoveAllNavMeshData();
            if (sources == null)
            {
                var markups = new List<NavMeshBuildMarkup>();
                sources = new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources(world, LayerMask.GetMask("World"), NavMeshCollectGeometry.RenderMeshes, 0, markups, sources);
            }
            var renderers = world.gameObject.GetComponentsInChildren<MeshRenderer>().Where(mr => mr?.gameObject?.layer == LayerMask.NameToLayer("World"));
            var bounds = renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Log("Computing Bounds for triangulation");
            var nvd = NavMeshBuilder.BuildNavMeshData(buildSettings, sources, bounds, world.position, world.rotation);
            Log("Building NavMeshData");
            NavMesh.AddNavMeshData(nvd);
            Log("Adding NavMeshData");
            var triangulation = NavMesh.CalculateTriangulation();
            Log("Calculating Triangulation");
            NavMesh.RemoveAllNavMeshData();
            Log("Triangulation cleanup");
            return triangulation;
        }

        NodeGraph SaveGraph(string name, Node[] nodes, NodeGraph.Link[] links)
        {
            var activeScene = SceneManager.GetActiveScene();
            var scenePath = activeScene.path;
            scenePath = System.IO.Path.GetDirectoryName(scenePath);
            var nodeGraphPath = System.IO.Path.Combine(scenePath, activeScene.name, $"{name}.asset");

            var nodeGraph = AssetDatabase.LoadAssetAtPath<NodeGraph>(nodeGraphPath);
            var isNew = false;
            if (!nodeGraph)
            {
                nodeGraph = CreateInstance<NodeGraph>();
                nodeGraph.name = name;
                isNew = true;
            }

            NodesField.SetValue(nodeGraph, nodes);
            LinksField.SetValue(nodeGraph, links);
            Log("Node and Link data assginment");

            if (isNew)
            {
                if (!AssetDatabase.IsValidFolder(System.IO.Path.Combine(scenePath, activeScene.name)))
                    AssetDatabase.CreateFolder(scenePath, activeScene.name);

                AssetDatabase.CreateAsset(nodeGraph, nodeGraphPath);
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.SetDirty(nodeGraph);
                var so = new SerializedObject(nodeGraph);
                so.ApplyModifiedProperties();
            }

            return nodeGraph;
        }

        void Log(string stepName)
        {
            if (DebugMode)
            {
                watcher.Stop();
                Debug.Log($"{stepName} took: {watcher.Elapsed.TotalMilliseconds}ms");
                watcher.Reset();
                watcher.Start();
            }
        }
    }

    internal class NavProjection
    {
        public List<triangle> triangles = new List<triangle>();
        public Dictionary<int, List<triangle>> lookup = new Dictionary<int, List<triangle>>();
        public Vector3[] vertices;
        public int[] indices;

        public bool Contains(Node node)
        {
            return triangles.Any(triangle =>
            {
                var pointOnPlane = triangle.Plane.ClosestPointOnPlane(node.position);
                var inTriangle = PointInTriangle(pointOnPlane, vertices[triangle.IndexA], vertices[triangle.IndexB], vertices[triangle.IndexC]);
                return inTriangle;
            });
        }
    }

    internal class triangle : System.IEquatable<triangle>
    {
        public int IndexA, IndexB, IndexC;
        public triangle NeighborAB, NeighborBC, NeighborCA;
        public Plane Plane;

        public triangle Opposite(int index)
        {
            if (IndexA == index) return NeighborBC;
            if (IndexB == index) return NeighborCA;
            if (IndexC == index) return NeighborAB;
            return null;
        }
        public (int a, int b) OppositeEdge(int index)
        {
            if (IndexA == index) return (IndexB, IndexC);
            if (IndexB == index) return (IndexC, IndexA);
            if (IndexC == index) return (IndexA, IndexB);
            return (-1, -1);
        }

        public int OppositeIndex(int index)
        {
            var target = Opposite(index);
            if (target?.NeighborBC == this) return target.IndexA;
            if (target?.NeighborAB == this) return target.IndexC;
            if (target?.NeighborCA == this) return target.IndexB;

            return -1;
        }

        public bool ContainsVertex(int index)
        {
            if (IndexA == index) return true;
            if (IndexB == index) return true;
            if (IndexC == index) return true;
            return false;
        }

        public bool IsNeighbor(triangle other) => NeighborAB == other || NeighborBC == other || NeighborCA == other;

        public int Neighbors() => (NeighborAB == null ? 0 : 1) + (NeighborBC == null ? 0 : 1) + (NeighborCA == null ? 0 : 1);

        public void AssignNeighbor(triangle other)
        {
            if (this == other) return;

            if (other.IndexA == IndexA && other.IndexB == IndexB && other.IndexC == IndexC) return;
            if (
                (IndexA == other.IndexA && IndexB == other.IndexB) ||
                (IndexA == other.IndexB && IndexB == other.IndexA) ||
                (IndexA == other.IndexA && IndexB == other.IndexC) ||
                (IndexA == other.IndexC && IndexB == other.IndexA) ||
                (IndexA == other.IndexB && IndexB == other.IndexC) ||
                (IndexA == other.IndexC && IndexB == other.IndexB)
               )
                NeighborAB = other;

            if (
                (IndexB == other.IndexA && IndexC == other.IndexB) ||
                (IndexB == other.IndexB && IndexC == other.IndexA) ||
                (IndexB == other.IndexA && IndexC == other.IndexC) ||
                (IndexB == other.IndexC && IndexC == other.IndexA) ||
                (IndexB == other.IndexB && IndexC == other.IndexC) ||
                (IndexB == other.IndexC && IndexC == other.IndexB)
               )
                NeighborBC = other;

            if (
                (IndexC == other.IndexA && IndexA == other.IndexB) ||
                (IndexC == other.IndexB && IndexA == other.IndexA) ||
                (IndexC == other.IndexA && IndexA == other.IndexC) ||
                (IndexC == other.IndexC && IndexA == other.IndexA) ||
                (IndexC == other.IndexB && IndexA == other.IndexC) ||
                (IndexC == other.IndexC && IndexA == other.IndexB)
               )
                NeighborCA = other;

            if (IsNeighbor(other) && !other.IsNeighbor(this))
                other.AssignNeighbor(this);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as triangle);
        }

        public bool Equals(triangle other)
        {
            return other != null &&
                   IndexA == other.IndexA &&
                   IndexB == other.IndexB &&
                   IndexC == other.IndexC;
        }

        public override int GetHashCode()
        {
            int hashCode = 738783513;
            hashCode = hashCode * -1521134295 + IndexA.GetHashCode();
            hashCode = hashCode * -1521134295 + IndexB.GetHashCode();
            hashCode = hashCode * -1521134295 + IndexC.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(triangle left, triangle right)
        {
            return EqualityComparer<triangle>.Default.Equals(left, right);
        }

        public static bool operator !=(triangle left, triangle right)
        {
            return !(left == right);
        }
    }
    internal static class Extensions
    {

        public static IEnumerable<triangle> Contains(this IEnumerable<triangle> triangles, Vector3[] vertices, Node node)
        {
            return triangles.Where(triangle =>
            {
                var pointOnPlane = triangle.Plane.ClosestPointOnPlane(node.position);
                var inTriangle = PointInTriangle(pointOnPlane, vertices[triangle.IndexA], vertices[triangle.IndexB], vertices[triangle.IndexC]);
                return inTriangle;
            });
        }


        public static bool PointInTriangle(Vector3 P, params Vector3[] TriangleVectors)
        {
            Vector3 A = TriangleVectors[0], B = TriangleVectors[1], C = TriangleVectors[2];
            if (SameSide(P, A, B, C) && SameSide(P, B, A, C) && SameSide(P, C, A, B))
            {
                Vector3 vc1 = Vector3.Cross(A - B, A - C);
                if (Mathf.Abs(Vector3.Dot(A - P, vc1)) <= .01f)
                    return true;
            }

            return false;
        }

        public static bool SameSide(Vector3 p1, Vector3 p2, Vector3 A, Vector3 B)
        {
            Vector3 cp1 = Vector3.Cross(B - A, p1 - A);
            Vector3 cp2 = Vector3.Cross(B - A, p2 - A);
            if (Vector3.Dot(cp1, cp2) >= 0) return true;
            return false;
        }

    }
}
