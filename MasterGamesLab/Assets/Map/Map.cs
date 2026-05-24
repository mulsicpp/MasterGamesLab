using System;
using System.Collections.Generic;
using InGameCamera;
using Map.GeometryGeneration;
using Unity.Burst.CompilerServices;
using Map.Infrastructure;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor.PackageManager;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using Map.Fleet;
using System.CodeDom;

namespace Map
{
    public class Map : NetworkBehaviour, IMap
    {
        public const int ID_OFFSET = 1;
        private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
        private static readonly int ProjectionFactor = Shader.PropertyToID("_ProjectionFactor");
        private static readonly int ProjectionCenter = Shader.PropertyToID("_ProjectionCenter");

        public static Map Instance { get; private set; } = null!;

        public IReadOnlyList<ITile> Tiles => tiles;
        public IReadOnlyList<ITile> ActiveTiles => activeTiles;
        public float Radius => radius;
        public int Resolution => resolution;

        [SerializeField]
        private Timestamp timestamp = new Timestamp(0);
        public Timestamp Timestamp { get => timestamp; }

        public IReadOnlyList<Edge> Edges => edges;
        public IReadOnlyInfrastructure Infrastructure => infrastructure;
        public IReadOnlyFleet Fleet => fleet;

        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private GameObject chunkPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        // --- DEBUG PATHFINDING TESTING FIELDS ---
        [Header("Pathfinding Debugger")]
        [Tooltip("Drag a Tile reference here, or use the context menu via the Inspector dots to test.")]
        [SerializeField] public int testStartTileId = -1;
        [SerializeField] public int testTargetTileId = -1;

        // Two independent trace buffers so both paths can be drawn at once
        private readonly List<Edge> shortestDebugPathEdges = new();
        private readonly List<Edge> cheapestDebugPathEdges = new();
        // ----------------------------------------

        private List<Tile> tiles;
        private List<Tile> activeTiles;
        private List<MapChunk> chunks;
        private float oldProjectionFactor;
        private Vector3 oldProjectionCenter;
        private int currentlyHoveredTileId;

        private Edge[] edges;
        private Infrastructure.Infrastructure infrastructure;
        private Fleet.Fleet fleet;

        // --- HIGH PERFORMANCE PRE-ALLOCATED RUNTIME BUFFERS ---
        private NodeState[] nodeStatesBuffer;
        private bool[] visitedTilesBuffer;
        private PriorityQueue<Tile, int, int> tileQueue;

        private void OnEnable()
        {
            Instance = this;
        }

        private void Start()
        {
            currentlyHoveredTileId = -1;
            Debug.Log("Starting Map Generation");
            var (chunksPoints, numPoints) = HexagonalSphere.GenerateIcoSphereChunks(radius, resolution);
            tiles = new List<Tile>(numPoints);
            chunks = new List<MapChunk>(chunksPoints.Count);

            edges = new Edge[0];
            infrastructure = new Infrastructure.Infrastructure();
            fleet = new Fleet.Fleet();

            var currentId = 0;
            foreach (var chunkPoints in chunksPoints)
            {
                var chunkGameObject = Instantiate(chunkPrefab, transform);
                var chunk = chunkGameObject.GetComponent<MapChunk>();
                var startId = currentId;

                foreach (var point in chunkPoints)
                {
                    point.InitializeTile(new TileId(currentId++), radius, chunk);
                    tiles.Add(point);
                }

                chunk.Init(this, startId, currentId);
                chunks.Add(chunk);
            }

            foreach (var tile in tiles)
            {
                tile.InitializeNeighbors();
            }

            ProceduralMapGenerator.GenerateMap();

            foreach (var chunk in chunks)
            {
                chunk.UpdateMesh();
            }

            Debug.Log($"Generated {tiles.Count} tiles");

            activeTiles = new List<Tile>();

            Shader.SetGlobalFloat(PlanetRadius, radius);

            // Allocate fixed-size index lookup buffers once at startup to keep garbage collection at 0
            nodeStatesBuffer = new NodeState[tiles.Count];
            visitedTilesBuffer = new bool[tiles.Count];
            tileQueue = new PriorityQueue<Tile, int, int>();
        }

        private void Update()
        {
            foreach (var chunk in chunks)
            {
                if (chunk.GeometryChanged)
                {
                    chunk.UpdateMesh();
                }
                else if (chunk.Dirty)
                {
                    chunk.UpdateTileData();
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                MainCamera.Instance.RequestCurrentlyHoveredTile(OnReadbackComplete);

                // Optional dynamic update line: recalculates debug paths on click if paths exist
                if (shortestDebugPathEdges.Count > 0 || cheapestDebugPathEdges.Count > 0)
                {
                    RecalculateDebugPaths();
                }
            }
            MainCamera.Instance.RequestCurrentlyHoveredTile(OnReadbackComplete);

            UpdateProjectionUniforms();
        }

        public ITile GetCurrentlyHoveredTile()
        {
            return currentlyHoveredTileId == -1 ? null : tiles[currentlyHoveredTileId];
        }

        public void AddActiveTile(Tile tile) => activeTiles.Add(tile);
        public void RemoveActiveTile(Tile tile) => activeTiles.Remove(tile);

        private void UpdateProjectionUniforms()
        {
            var projectionCenter = (MainCamera.Instance.CurrentPosition - transform.position).normalized;
            var currentDistance = MainCamera.Instance.CurrentDistance;
            var projectionFactor = (currentDistance - fullSphereDistance) /
                                   (fullProjectionDistance - fullSphereDistance);

            projectionFactor = Mathf.Clamp01(projectionFactor);

            if (oldProjectionCenter == projectionCenter && Mathf.Approximately(oldProjectionFactor, projectionFactor))
            {
                return;
            }

            Shader.SetGlobalVector(ProjectionCenter, projectionCenter);
            Shader.SetGlobalFloat(ProjectionFactor, projectionFactor);

            oldProjectionCenter = projectionCenter;
            oldProjectionFactor = projectionFactor;
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                currentlyHoveredTileId = -1;
                return;
            }

            var colorData = request.GetData<Color32>();
            var pixelColor = colorData[0];

            currentlyHoveredTileId = ((pixelColor.r << 16) | (pixelColor.g << 8) | pixelColor.b) - ID_OFFSET;
        }

        public void Generate(int seed)
        {
            Debug.Log("Generating world with seed " + seed + " ...");

            foreach (var tile in tiles)
            {
                if (tile.PositionOnSphere.z < -0.97f) tile.Type = Tile.TileType.Mountain;
                else if (tile.PositionOnSphere.z < -0.9f) tile.Type = Tile.TileType.Forest;
                else if (tile.PositionOnSphere.z < -0.7f) tile.Type = Tile.TileType.Plain;
                else tile.Type = Tile.TileType.Water;
            }

            InitEdges();

            infrastructure = new Infrastructure.Infrastructure();
            fleet = new Fleet.Fleet();

            foreach (var chunk in chunks)
            {
                chunk.UpdateMesh();
            }

            Infrastructure.SpawnLocal(new Producer.ProducerState { Common = { TileId = edges[0].EndTile.Id }, Good = Good.Apple });

            // Test edge types

            // for(int i = 0; i < edges.Length; i++)
            // {
            //     if (i < edges.Length / 8) SetEdge(new EdgeId(i), Edge.EdgeType.Rail, PlayerId.NONE);
            //     else if (i < edges.Length / 6) SetEdge(new EdgeId(i), Edge.EdgeType.Canal, PlayerId.NONE);
            //     else if(i < edges.Length / 4) SetEdge(new EdgeId(i), Edge.EdgeType.Road, PlayerId.NONE);
            // }
        }

        private void InitEdges()
        {
            var tempEdges = new List<Edge>();

            foreach (Tile t in tiles) t.ClearEdges();
            foreach (Tile t in tiles) t.InitializeEdges(tempEdges);

            Debug.Log("Initialized " + tempEdges.Count + " edges");

            edges = tempEdges.ToArray();
        }

        public bool UpdateEdge(Edge.EdgeState edgeState)
        {
            if (edgeState.ArrayIndex >= edges.Length || edgeState.ArrayIndex < 0) return false;

            var edge = edges[edgeState.ArrayIndex];

            if (!edge.CanBecomeType(edgeState.Type)) return false;

            edge.State = edgeState;
            return true;
        }


        private ClientRpcParams GetRpcParams(ClientId clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new List<ulong> { clientId },
                }
            };
        }

        public void SyncClientMap(Timestamp clientTimestamp, ClientId clientId)
        {
            if (!IsServer) return;

            var rpcParams = GetRpcParams(clientId);

            SyncClientObjects<Edge, Edge.EdgeState>(clientTimestamp, rpcParams, edges, Constants.MAX_EDGES_PER_RPC);

            SyncClientObjects<Producer, Producer.ProducerState>(clientTimestamp, rpcParams, infrastructure.Producers, Constants.MAX_PRODUCERS_PER_RPC);
            SyncClientObjects<Consumer, Consumer.ConsumerState>(clientTimestamp, rpcParams, infrastructure.Consumers, Constants.MAX_CONSUMERS_PER_RPC);
        }

        private void SyncClientObjects<T, U>(Timestamp clientTimestamp, ClientRpcParams rpcParams, IEnumerable<T> objects, int maxObjects = 32) where U : struct, IState where T : ISynchableObject<U>
        {
            var updatedStates = new List<U>();
            updatedStates.Capacity = maxObjects;

            foreach (var obj in objects)
            {
                if (obj.Timestamp > clientTimestamp)
                {
                    updatedStates.Add(obj.State);
                }

                if (updatedStates.Count == maxObjects)
                {
                    UpdateGenericStatesClient(Timestamp, updatedStates.ToArray(), rpcParams);
                    updatedStates.Clear();
                }
            }

            if (updatedStates.Count > 0)
            {
                UpdateGenericStatesClient(Timestamp, updatedStates.ToArray(), rpcParams);
            }
        }

        public void UpdateGenericStatesClient<T>(Timestamp timestamp, T[] states, ClientRpcParams rpcParams = default) where T : struct, IState
        {
            if (!IsServer) return;
            // edges
            if (states is Edge.EdgeState[] e) UpdateEdgeStatesClientRpc(timestamp, e, rpcParams);

            // structures
            else if (states is Producer.ProducerState[] p) UpdateProducerStatesClientRpc(timestamp, p, rpcParams);
            else if (states is Consumer.ConsumerState[] c) UpdateConsumerStatesClientRpc(timestamp, c, rpcParams);
        }



        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateEdgeStatesClientRpc(Timestamp timestamp, Edge.EdgeState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Edges, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateProducerStatesClientRpc(Timestamp timestamp, Producer.ProducerState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Infrastructure.Producers, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateConsumerStatesClientRpc(Timestamp timestamp, Consumer.ConsumerState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Infrastructure.Consumers, states);



        private void UpdateGenericStatesLocal<T, U>(Timestamp timestamp, IReadOnlyList<T> objects, U[] states) where U : struct, IState where T : ISynchableObject<U>
        {
            this.timestamp = timestamp;
            foreach (var state in states)
            {
                objects[state.ArrayIndex].State = state;
            }
        }


        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestNewEdgesServerRpc(Edge.EdgeType edgeType, EdgeId[] edgeIds, RpcParams rpcParams = default)
        {
            var playerId = PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received new edges request from player " + playerId.Value);

            if (playerId == PlayerId.NONE) return;

            var validPath = true;
            foreach (var id in edgeIds)
            {
                if (id >= edges.Length || id < 0)
                {
                    Debug.Log("Id out of range: " + id.Value);
                    validPath = false;
                    break;
                }

                var edge = edges[id];
                if (!edge.CanBecomeType(edgeType))
                {
                    Debug.Log("Edge cannot become type: " + id.Value);
                    validPath = false;
                    break;
                }
            }

            Debug.Log("Path is valid: " + validPath);
            if (!validPath) return;

            Edge.EdgeState[] edgeStates = new Edge.EdgeState[edgeIds.Length];
            for (var i = 0; i < edgeIds.Length; i++)
            {
                edgeStates[i] = new Edge.EdgeState { Id = edgeIds[i], Type = edgeType, Owner = playerId };
            }

            var nextTimestamp = Timestamp.Next();
            UpdateEdgeStatesClientRpc(nextTimestamp, edgeStates);
        }

        private struct NodeState
        {
            public int RealDistance;
            public int RealCost;
            public int CameFromId;
            public EdgeId ReachedViaEdgeId;
        }

        public enum RoutePriorityMode { Shortest, Cheapest }

        // 1. High-Performance Shortest Path (Distance First, Array Backed)
        public EdgeId[] FindShortestPath(Tile start, Tile target)
        {
            if (!IsValidRequest(start, target)) return null;

            System.Array.Clear(visitedTilesBuffer, 0, visitedTilesBuffer.Length);
            tileQueue.Clear();

            int startId = start.Id.Value;
            nodeStatesBuffer[startId] = new NodeState { RealCost = 0, RealDistance = 0, CameFromId = -1 };
            visitedTilesBuffer[startId] = true;
            tileQueue.Enqueue(start, 0, 0);

            while (tileQueue.Count > 0)
            {
                Tile current = tileQueue.Dequeue();
                int currentId = current.Id.Value;

                if (current == target) return ReconstructPathArray(startId, target.Id.Value);

                int currentRealDistance = nodeStatesBuffer[currentId].RealDistance;
                int currentRealCost = nodeStatesBuffer[currentId].RealCost;

                foreach (Edge edge in current.Edges)
                {
                    if (edge.Type != Edge.EdgeType.Road) continue;
                    Tile neighbor = (edge.StartTile == current) ? edge.EndTile : edge.StartTile;
                    int neighborId = neighbor.Id.Value;

                    int newRealDistance = currentRealDistance + Constants.ROAD_MOVEMENT_DISTANCE;
                    int newRealCost = currentRealCost + ((edge.Owner == PlayerId.NONE || edge.Owner != PlayerManager.Instance.SelfId) ? Constants.ROAD_MOVEMENT_COST : 0);

                    bool hasState = visitedTilesBuffer[neighborId];

                    if (!hasState || newRealDistance < nodeStatesBuffer[neighborId].RealDistance ||
                       (newRealDistance == nodeStatesBuffer[neighborId].RealDistance && newRealCost < nodeStatesBuffer[neighborId].RealCost))
                    {
                        visitedTilesBuffer[neighborId] = true;
                        nodeStatesBuffer[neighborId] = new NodeState
                        {
                            RealCost = newRealCost,
                            RealDistance = newRealDistance,
                            CameFromId = currentId,
                            ReachedViaEdgeId = edge.Id
                        };
                        tileQueue.Enqueue(neighbor, newRealDistance + GetSphericalHeuristic(neighbor, target), newRealCost);
                    }
                }
            }
            return null;
        }

        // 2. High-Performance Cheapest Path (Cost First, Array Backed)
        public EdgeId[] FindCheapestPath(Tile start, Tile target)
        {
            if (!IsValidRequest(start, target)) return null;

            System.Array.Clear(visitedTilesBuffer, 0, visitedTilesBuffer.Length);
            tileQueue.Clear();

            int startId = start.Id.Value;
            nodeStatesBuffer[startId] = new NodeState { RealCost = 0, RealDistance = 0, CameFromId = -1 };
            visitedTilesBuffer[startId] = true;
            tileQueue.Enqueue(start, 0, 0);

            while (tileQueue.Count > 0)
            {
                Tile current = tileQueue.Dequeue();
                int currentId = current.Id.Value;

                if (current == target) return ReconstructPathArray(startId, target.Id.Value);

                int currentRealDistance = nodeStatesBuffer[currentId].RealDistance;
                int currentRealCost = nodeStatesBuffer[currentId].RealCost;

                foreach (Edge edge in current.Edges)
                {
                    if (edge.Type != Edge.EdgeType.Road) continue;
                    Tile neighbor = (edge.StartTile == current) ? edge.EndTile : edge.StartTile;
                    int neighborId = neighbor.Id.Value;

                    int newRealDistance = currentRealDistance + Constants.ROAD_MOVEMENT_DISTANCE;
                    int newRealCost = currentRealCost + ((edge.Owner == PlayerId.NONE || edge.Owner != PlayerManager.Instance.SelfId) ? Constants.ROAD_MOVEMENT_COST : 0);

                    bool hasState = visitedTilesBuffer[neighborId];

                    if (!hasState || newRealCost < nodeStatesBuffer[neighborId].RealCost ||
                       (newRealCost == nodeStatesBuffer[neighborId].RealCost && newRealDistance < nodeStatesBuffer[neighborId].RealDistance))
                    {
                        visitedTilesBuffer[neighborId] = true;
                        nodeStatesBuffer[neighborId] = new NodeState
                        {
                            RealCost = newRealCost,
                            RealDistance = newRealDistance,
                            CameFromId = currentId,
                            ReachedViaEdgeId = edge.Id
                        };
                        tileQueue.Enqueue(neighbor, newRealCost, newRealDistance + GetSphericalHeuristic(neighbor, target));
                    }
                }
            }
            return null;
        }

        private bool IsValidRequest(Tile start, Tile target)
        {
            if (start == null || target == null || start == target) return false;
            if (start.Type == Tile.TileType.Water || target.Type == Tile.TileType.Water) return false;
            if (start.Type == Tile.TileType.Mountain || target.Type == Tile.TileType.Mountain) return false;
            return true;
        }

        private EdgeId[] ReconstructPathArray(int startId, int targetId)
        {
            List<EdgeId> result = new();
            int currId = targetId;
            while (currId != startId)
            {
                result.Add(nodeStatesBuffer[currId].ReachedViaEdgeId);
                currId = nodeStatesBuffer[currId].CameFromId;
            }
            result.Reverse();
            return result.ToArray();
        }

        private int GetSphericalHeuristic(Tile current, Tile target)
        {
            if (current == target) return 0;

            Vector3 v1 = current.PositionOnSphere.normalized;
            Vector3 v2 = target.PositionOnSphere.normalized;
            float angleRadians = Mathf.Acos(Mathf.Clamp(Vector3.Dot(v1, v2), -1f, 1f));

            // Estimates steps over arc surface
            float approximateTileAngleRad = Mathf.PI / (resolution * 2.0f);
            return Mathf.FloorToInt(angleRadians / approximateTileAngleRad);
        }

        // --- IN-EDITOR RUNTIME TESTING TOOLS ---

        [ContextMenu("Test Path Between IDs")]
        public void RecalculateDebugPaths()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Please enter Play Mode before executing a pathfinding calculation test.");
                return;
            }

            if (tiles == null || testStartTileId < 0 || testStartTileId >= tiles.Count || testTargetTileId < 0 || testTargetTileId >= tiles.Count)
            {
                Debug.LogError("Invalid Node ID ranges configured inside the Map components debug window.");
                return;
            }

            shortestDebugPathEdges.Clear();
            cheapestDebugPathEdges.Clear();

            Tile startTile = tiles[testStartTileId];
            Tile targetTile = tiles[testTargetTileId];

            // 1. Calculate Shortest Path (Green Profile)
            System.Diagnostics.Stopwatch swShortest = System.Diagnostics.Stopwatch.StartNew();
            EdgeId[] shortestResult = FindShortestPath(startTile, targetTile);
            swShortest.Stop();

            if (shortestResult != null)
            {
                Debug.Log($"[A* Shortest] <color=lime>Success!</color> Found route containing {shortestResult.Length} steps in <b>{swShortest.Elapsed.TotalMilliseconds:F4} ms</b>.");
                foreach (var id in shortestResult) shortestDebugPathEdges.Add(edges[id.Value]);
            }

            // 2. Calculate Cheapest Path (Red Profile)
            System.Diagnostics.Stopwatch swCheapest = System.Diagnostics.Stopwatch.StartNew();
            EdgeId[] cheapestResult = FindCheapestPath(startTile, targetTile);
            swCheapest.Stop();

            if (cheapestResult != null)
            {
                Debug.Log($"[A* Cheapest] <color=orange>Success!</color> Found route containing {cheapestResult.Length} steps in <b>{swCheapest.Elapsed.TotalMilliseconds:F4} ms</b>.");
                foreach (var id in cheapestResult) cheapestDebugPathEdges.Add(edges[id.Value]);
            }

            if (shortestResult == null && cheapestResult == null)
            {
                Debug.LogWarning($"Pathfinding failed or route isolated between Node {testStartTileId} and Node {testTargetTileId}. Check terrain settings.");
            }
        }

        // ---------------------------------------

        public void OnDrawGizmos()
        {
            if (edges == null) return;

            List<Vector3> nonePoints = new List<Vector3>();
            List<Vector3> roadPoints = new List<Vector3>();
            List<Vector3> canalPoints = new List<Vector3>();
            List<Vector3> railPoints = new List<Vector3>();

            for (int i = 0; i < edges.Length; i++)
            {
                List<Vector3> points = nonePoints;
                switch (edges[i].Type)
                {
                    case Edge.EdgeType.Road: points = roadPoints; break;
                    case Edge.EdgeType.Canal: points = canalPoints; break;
                    case Edge.EdgeType.Rail: points = railPoints; break;
                }
                points.Add(edges[i].StartTile.PositionOnSphere * 1.01f);
                points.Add(edges[i].EndTile.PositionOnSphere * 1.01f);
            }

            Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.05f);
            Gizmos.DrawLineList(nonePoints.ToArray().AsSpan());

            Gizmos.color = Color.black;
            Gizmos.DrawLineList(roadPoints.ToArray().AsSpan());

            Gizmos.color = Color.blue;
            Gizmos.DrawLineList(canalPoints.ToArray().AsSpan());

            Gizmos.color = new Color(0.1f, 0.1f, 0.1f);
            Gizmos.DrawLineList(railPoints.ToArray().AsSpan());

            // --- SHORTEST PATH RENDERING LAYER (GREEN) ---
            if (shortestDebugPathEdges != null && shortestDebugPathEdges.Count > 0)
            {
                Gizmos.color = Color.green;
                foreach (var edge in shortestDebugPathEdges)
                {
                    // Scaled at 1.015f to sit slightly above standard roads
                    Vector3 p1 = edge.StartTile.PositionOnSphere * 1.015f;
                    Vector3 p2 = edge.EndTile.PositionOnSphere * 1.015f;

                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawSphere(p1, radius * 0.006f);
                    Gizmos.DrawSphere(p2, radius * 0.006f);
                }
            }

            // --- CHEAPEST PATH RENDERING LAYER (RED) ---
            if (cheapestDebugPathEdges != null && cheapestDebugPathEdges.Count > 0)
            {
                Gizmos.color = Color.red;
                foreach (var edge in cheapestDebugPathEdges)
                {
                    // Scaled slightly higher (1.018f) so overlapping paths don't Z-fight in the viewport
                    Vector3 p1 = edge.StartTile.PositionOnSphere * 1.018f;
                    Vector3 p2 = edge.EndTile.PositionOnSphere * 1.018f;

                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawSphere(p1, radius * 0.006f);
                    Gizmos.DrawSphere(p2, radius * 0.006f);
                }
            }

            var orange = new Color(1.0f, 0.2f, 0.0f); 

            foreach (var producer in infrastructure.Producers)
                if (producer.Tile != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(producer.Tile.PositionOnSphere, 0.015f);
                    if(producer.Good != Good.None)
                    {
                        switch(producer.Good)
                        {
                            case Good.Apple: Gizmos.color = Color.red; break;
                            case Good.Orange: Gizmos.color = orange; break;
                            case Good.Banana: Gizmos.color = Color.yellow; break;
                        }
                        Gizmos.DrawSphere(producer.Tile.PositionOnSphere * 1.02f, 0.005f);
                    }
                }

            foreach (var consumer in infrastructure.Consumers)
                if (consumer.Tile != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(consumer.Tile.PositionOnSphere, 0.015f);
                    if (consumer.RequestedGood != Good.None)
                    {
                        switch (consumer.RequestedGood)
                        {
                            case Good.Apple: Gizmos.color = Color.red; break;
                            case Good.Orange: Gizmos.color = orange; break;
                            case Good.Banana: Gizmos.color = Color.yellow; break;
                        }
                        Gizmos.DrawSphere(consumer.Tile.PositionOnSphere * 1.02f, 0.005f);
                    }
                }

            // Debug.Log("Drawing gizmos");
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Instance = null!;
        }
#endif
    }
}