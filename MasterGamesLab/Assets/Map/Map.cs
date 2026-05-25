using System.Collections.Generic;
using InGameCamera;
using Map.GeometryGeneration;
using Map.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Map.Fleet;
using System;
using Unity.Collections;
using System.Data;

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

            ProceduralMapGenerator.GenerateMap(this);

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

        public void Tick()
        {
            uint tickRate = NetworkManager.Singleton.NetworkTickSystem.TickRate;
            float tickDuration = 1.0f / tickRate;

            Debug.Log("Map Tick");

            foreach (var vehicle in Fleet.Vehicles)
            {
                vehicle.Tick(tickDuration);
            }

            UpdateDirtyObjectsOnClient();
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

            Predicate<Timestamped> condition = obj => obj.Timestamp > clientTimestamp;

            SyncObjectsOnClientFiltered<Edge, Edge.EdgeState>(edges, condition, rpcParams);

            SyncObjectsOnClientFiltered<Producer, Producer.ProducerState>(infrastructure.Producers, condition, rpcParams);
            SyncObjectsOnClientFiltered<Consumer, Consumer.ConsumerState>(infrastructure.Consumers, condition, rpcParams);

            SyncObjectsOnClientFiltered<Truck, Truck.TruckState>(fleet.Trucks, condition, rpcParams);
        }

        public void UpdateDirtyObjectsOnClient()
        {
            if (!IsServer) return;

            Predicate<Timestamped> condition = obj => obj.Dirty;

            timestamp = Timestamp.Next();
            SyncObjectsOnClientFiltered<Edge, Edge.EdgeState>(edges, condition);

            timestamp = Timestamp.Next();
            SyncObjectsOnClientFiltered<Producer, Producer.ProducerState>(infrastructure.Producers, condition);
            timestamp = Timestamp.Next();
            SyncObjectsOnClientFiltered<Consumer, Consumer.ConsumerState>(infrastructure.Consumers, condition);

            timestamp = Timestamp.Next();
            SyncObjectsOnClientFiltered<Vehicle, Vehicle.VehicleProgressState>(fleet.Vehicles, v => v.ProgressDirty);
            timestamp = Timestamp.Next();
            SyncObjectsOnClientFiltered<Truck, Truck.TruckState>(fleet.Trucks, condition);
        }

        public void SyncObjectsOnClientFiltered<T, U>(IEnumerable<T> objects, Predicate<T> condition, ClientRpcParams rpcParams = default) where U : struct, IState where T : ISynchableObject<U>
        {
            int currentSize = 0;
            List<U> states = new List<U>();

            foreach (var obj in objects)
            {
                if (!condition(obj)) continue;
                var state = obj.State;
                int objSize = state.SerializedSize;

                if (objSize > Constants.MAX_SYNC_STATE_BYTES_PER_RPC)
                {
                    if (states.Count > 0)
                    {
                        UpdateGenericStatesOnClient(Timestamp, states.ToArray(), rpcParams);
                        states.Clear();
                        currentSize = 0;
                    }
                    UpdateGenericStatesOnClient(Timestamp, new U[] { state }, rpcParams);
                    continue;
                }

                if (currentSize + objSize > Constants.MAX_SYNC_STATE_BYTES_PER_RPC)
                {
                    UpdateGenericStatesOnClient(Timestamp, states.ToArray(), rpcParams);
                    states.Clear();
                    currentSize = 0;
                }

                states.Add(state);
                currentSize += objSize;
            }

            if (states.Count > 0)
            {
                UpdateGenericStatesOnClient(Timestamp, states.ToArray(), rpcParams);
            }
        }

        public void UpdateGenericStatesOnClient<T>(Timestamp timestamp, T[] states, ClientRpcParams rpcParams = default) where T : struct, IState
        {
            if (!IsServer) return;
            // edges
            if (states is Edge.EdgeState[] e) UpdateEdgeStatesClientRpc(timestamp, e, rpcParams);

            // structures
            else if (states is Producer.ProducerState[] p) UpdateProducerStatesClientRpc(timestamp, p, rpcParams);
            else if (states is Consumer.ConsumerState[] c) UpdateConsumerStatesClientRpc(timestamp, c, rpcParams);

            // vehicles
            else if (states is Vehicle.VehicleProgressState[] progress) UpdateVehicleProgressStatesClientRpc(timestamp, progress, rpcParams);
            else if (states is Truck.TruckState[] t) UpdateTruckStatesClientRpc(timestamp, t, rpcParams);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateEdgeStatesClientRpc(Timestamp timestamp, Edge.EdgeState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Edges, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateProducerStatesClientRpc(Timestamp timestamp, Producer.ProducerState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Infrastructure.Producers, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateConsumerStatesClientRpc(Timestamp timestamp, Consumer.ConsumerState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Infrastructure.Consumers, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateVehicleProgressStatesClientRpc(Timestamp timestamp, Vehicle.VehicleProgressState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Fleet.Vehicles, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateTruckStatesClientRpc(Timestamp timestamp, Truck.TruckState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, Fleet.Trucks, states);



        private void UpdateGenericStatesLocal<T, U>(Timestamp timestamp, IReadOnlyList<T> objects, U[] states) where U : struct, IState where T : Timestamped, ISynchableObject<U>
        {
            this.timestamp = timestamp;
            foreach (var state in states)
            {
                objects[state.ArrayIndex].ApplyServerState(state);
            }
        }


        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateVehicleProgressClientRpc(Timestamp timestamp, Vehicle.VehicleProgressState[] progressStates, ClientRpcParams rpcParams = default)
        {
            this.timestamp = timestamp;
            foreach (var state in progressStates)
            {
                fleet.UpdateVehicleProgress(state);
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

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestNewTruckServerRpc(TileId parkedTileId, RpcParams rpcParams = default)
        {
            var playerId = PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received new truck request from player " + playerId.Value);

            if (playerId == PlayerId.NONE) return;

            var truckState = new Truck.TruckState { Common = { Index = new((byte)fleet.GetFirstEmptyIndex(Vehicle.VehicleType.Truck)), Owner = playerId, ParkedTileId = parkedTileId }, Good = Good.None };

            var nextTimestamp = Timestamp.Next();
            UpdateTruckStatesClientRpc(nextTimestamp, new[] { truckState });
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestTruckRouteServerRpc(VehicleIndex index, TileId[] routeIds, RpcParams rpcParams = default)
        {
            var playerId = PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received new truck request from player " + playerId.Value);

            if (playerId == PlayerId.NONE) return;

            var truck = Fleet.Trucks[index];

            if (!truck.IsParked || truck.Owner != playerId) return;

            if (routeIds?.Length < 2) return;

            var truckState = truck.State;

            truckState.Common.ParkedTileId = TileId.NONE;
            truckState.Common.RouteIds = routeIds;
            truckState.Common.RouteProgress = 0.0f;

            var nextTimestamp = Timestamp.Next();
            UpdateTruckStatesClientRpc(nextTimestamp, new[] { truckState });
        }

        private struct NodeState
        {
            public int RealDistance;
            public int RealCost;
            public int CameFromId;
            public EdgeId ReachedViaEdgeId;
        }

        public enum RoutePriorityMode { Shortest, Cheapest }

        public EdgeId[] FindShortestPath(Tile start, Tile target, out TileId[] pathTiles)
        {
            pathTiles = null;
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

                if (current == target)
                {
                    pathTiles = ReconstructPathArrayTiles(startId, target.Id.Value);
                    return ReconstructPathArray(startId, target.Id.Value);
                }

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

        public EdgeId[] FindCheapestPath(Tile start, Tile target, out TileId[] pathTiles)
        {
            pathTiles = null;
            if (!IsValidRequest(start, target)) { pathTiles = null; return null; }

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

                if (current == target)
                {
                    pathTiles = ReconstructPathArrayTiles(startId, target.Id.Value);
                    return ReconstructPathArray(startId, target.Id.Value);
                }

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

        public EdgeId[] FindShortestPath(Tile start, Tile target) => FindShortestPath(start, target, out _);
        public EdgeId[] FindCheapestPath(Tile start, Tile target) => FindCheapestPath(start, target, out _);

        private bool IsValidRequest(Tile start, Tile target)
        {
            if (start == null || target == null || start == target) return false;
            if (start.Type == Tile.TileType.Water || target.Type == Tile.TileType.Water) return false;
            if (start.Type == Tile.TileType.Mountain || target.Type == Tile.TileType.Mountain) return false;
            return true;
        }

        private EdgeId[] ReconstructPathArray(int startId, int targetId)
        {
            var result = new List<EdgeId>();
            int currId = targetId;
            while (currId != startId)
            {
                result.Add(nodeStatesBuffer[currId].ReachedViaEdgeId);
                currId = nodeStatesBuffer[currId].CameFromId;
            }
            result.Reverse();
            return result.ToArray();
        }

        private TileId[] ReconstructPathArrayTiles(int startId, int targetId)
        {
            var result = new List<TileId>
            {
                new TileId(targetId)
            };

            int currId = targetId;
            while (currId != startId)
            {
                currId = nodeStatesBuffer[currId].CameFromId;
                result.Add(new TileId(currId));
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

            float approximateTileAngleRad = Mathf.PI / (resolution * 2.0f);
            return Mathf.FloorToInt(angleRadians / approximateTileAngleRad);
        }

        private Vector3 GetProjectedPosition(Vector3 positionOnSphere, float heightOffsetFactor = 1.0f)
        {
            // Safety check if parameters aren't initialized yet
            if (oldProjectionCenter == Vector3.zero)
                return positionOnSphere * heightOffsetFactor;

            // 1. Replicate exactly how the shader processes world space positions
            Vector3 worldPos = positionOnSphere * heightOffsetFactor;
            Vector3 projectionCenter = oldProjectionCenter.normalized;
            float sphereRadius = radius;
            float projectionFactor = oldProjectionFactor;

            // 2. Calculate projection mapping matching HLSL step-by-step
            Vector3 pNorm = worldPos.normalized; // Assuming planet center is at (0,0,0)
            float d = Vector3.Dot(projectionCenter, pNorm);
            d = Mathf.Clamp(d, -1.0f, 1.0f);
            float angle = Mathf.Acos(d);

            // Distance along the surface of the sphere
            float arcLength = sphereRadius * angle;

            // Direction outward from the focus point on the tangent plane
            Vector3 toPoint = pNorm - (projectionCenter * d);
            float lengthToPoint = toPoint.magnitude;

            Vector3 flatPos;
            if (lengthToPoint < 0.0001f)
            {
                flatPos = projectionCenter * sphereRadius; // Center point
            }
            else
            {
                Vector3 dirOnPlane = toPoint / lengthToPoint;
                flatPos = (projectionCenter * sphereRadius) + (dirOnPlane * arcLength);
            }

            // 3. Match shader height behavior: Extract elevation and push straight UP along projection axis
            float elevation = worldPos.magnitude - sphereRadius;
            flatPos += projectionCenter * elevation;

            // 4. Blend Position exactly like the shader's lerp loop
            return Vector3.Lerp(worldPos, flatPos, projectionFactor);
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

            System.Diagnostics.Stopwatch swShortest = System.Diagnostics.Stopwatch.StartNew();
            EdgeId[] shortestResult = FindShortestPath(startTile, targetTile);
            swShortest.Stop();

            if (shortestResult != null)
            {
                Debug.Log($"[A* Shortest] <color=lime>Success!</color> Found route containing {shortestResult.Length} steps in <b>{swShortest.Elapsed.TotalMilliseconds:F4} ms</b>.");
                foreach (var id in shortestResult) shortestDebugPathEdges.Add(edges[id.Value]);
            }

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

            // Draw default background grid lines and infrastructure paths
            for (int i = 0; i < edges.Length; i++)
            {
                switch (edges[i].Type)
                {
                    case Edge.EdgeType.Road:
                        Gizmos.color = Color.black;
                        break;
                    case Edge.EdgeType.Canal:
                        Gizmos.color = Color.blue;
                        break;
                    case Edge.EdgeType.Rail:
                        Gizmos.color = new Color(0.1f, 0.1f, 0.1f);
                        break;
                    default:
                        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.05f);
                        break;
                }

                // Project positions dynamically using current shader parameters
                Vector3 p1 = GetProjectedPosition(edges[i].StartTile.PositionOnSphere, 1.01f);
                Vector3 p2 = GetProjectedPosition(edges[i].EndTile.PositionOnSphere, 1.01f);
                Gizmos.DrawLine(p1, p2);
            }

            // --- SHORTEST PATH RENDERING LAYER (GREEN) ---
            if (shortestDebugPathEdges != null && shortestDebugPathEdges.Count > 0)
            {
                Gizmos.color = Color.green;
                foreach (var edge in shortestDebugPathEdges)
                {
                    Vector3 p1 = GetProjectedPosition(edge.StartTile.PositionOnSphere, 1.015f);
                    Vector3 p2 = GetProjectedPosition(edge.EndTile.PositionOnSphere, 1.015f);

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
                    Vector3 p1 = GetProjectedPosition(edge.StartTile.PositionOnSphere, 1.018f);
                    Vector3 p2 = GetProjectedPosition(edge.EndTile.PositionOnSphere, 1.018f);

                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawSphere(p1, radius * 0.006f);
                    Gizmos.DrawSphere(p2, radius * 0.006f);
                }
            }

            var orange = new Color(1.0f, 0.2f, 0.0f);

            // --- PRODUCERS ---
            foreach (var producer in infrastructure.Producers)
            {
                if (producer.Tile != null)
                {
                    Vector3 basePos = GetProjectedPosition(producer.Tile.PositionOnSphere, 1.0f);
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(basePos, 0.015f);

                    if (producer.Good != Good.None)
                    {
                        switch (producer.Good)
                        {
                            case Good.Apple: Gizmos.color = Color.red; break;
                            case Good.Orange: Gizmos.color = orange; break;
                            case Good.Banana: Gizmos.color = Color.yellow; break;
                        }
                        Vector3 cargoPos = GetProjectedPosition(producer.Tile.PositionOnSphere, 1.02f);
                        Gizmos.color = Gizmos.color; // Keep targeted good color context
                        Gizmos.DrawSphere(cargoPos, 0.005f);
                    }
                }
            }

            // --- CONSUMERS ---
            foreach (var consumer in infrastructure.Consumers)
            {
                if (consumer.Tile != null)
                {
                    Vector3 basePos = GetProjectedPosition(consumer.Tile.PositionOnSphere, 1.0f);
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(basePos, 0.015f);

                    if (consumer.RequestedGood != Good.None)
                    {
                        switch (consumer.RequestedGood)
                        {
                            case Good.Apple: Gizmos.color = Color.red; break;
                            case Good.Orange: Gizmos.color = orange; break;
                            case Good.Banana: Gizmos.color = Color.yellow; break;
                        }
                        Vector3 cargoPos = GetProjectedPosition(consumer.Tile.PositionOnSphere, 1.02f);
                        Gizmos.DrawSphere(cargoPos, 0.005f);
                    }
                }
            }

            foreach (var truck in Fleet.Trucks)
            {
                if (!truck.Exists) continue;
                Vector3 basePos = Vector3.zero;
                if (truck.IsParked)
                    basePos = GetProjectedPosition(truck.ParkedTile.PositionOnSphere, 1.0f);
                else if (truck.IsDriving)
                {
                    if (truck.RouteProgress <= 0.0f)
                        basePos = GetProjectedPosition(truck.Route[0].PositionOnSphere, 1.0f);
                    else if (truck.RouteProgress >= truck.Route.Length - 1)
                        basePos = GetProjectedPosition(truck.Route[truck.Route.Length - 1].PositionOnSphere, 1.0f);
                    else
                    {
                        int index = (int)truck.RouteProgress;
                        float localProgress = truck.RouteProgress - index;

                        var pos1 = GetProjectedPosition(truck.Route[index].PositionOnSphere, 1.0f);
                        var pos2 = GetProjectedPosition(truck.Route[index + 1].PositionOnSphere, 1.0f);

                        basePos = pos1 * (1.0f - localProgress) + pos2 * localProgress;
                    }
                }
                Gizmos.color = Constants.PLAYER_COLORS[truck.Owner % Constants.MAX_PLAYER_COUNT];
                Gizmos.DrawSphere(basePos, 0.01f);

                if (truck.Good != Good.None)
                {
                    switch (truck.Good)
                    {
                        case Good.Apple: Gizmos.color = Color.red; break;
                        case Good.Orange: Gizmos.color = orange; break;
                        case Good.Banana: Gizmos.color = Color.yellow; break;
                    }
                    Vector3 cargoPos = GetProjectedPosition(basePos, 1.02f);
                    Gizmos.DrawSphere(cargoPos, 0.005f);

                }
            }
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