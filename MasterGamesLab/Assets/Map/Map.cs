using System;
using System.Collections.Generic;
using System.Linq;
using InGameCamera;
using Map.GeometryGeneration;
using Map.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Map.Fleet;

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

        [SerializeField] private Timestamp timestamp = new Timestamp(0);
        public Timestamp Timestamp => timestamp;

        [SerializeField] public bool Running = true;

        public IReadOnlyList<Edge> Edges => edges;
        public IReadOnlyInfrastructure Infrastructure => infrastructure;
        public IReadOnlyFleet Fleet => fleet;

        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private GameObject chunkPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        public struct TreeData
        {
            public Vector3 Position;
            public Vector3 Normal;
            public float Scale;
            public float Yaw;
            public float Random;
            public float Active;
        }

        private List<Tile> tiles;
        private List<Tile> activeTiles;
        private List<MapChunk> chunks;
        private float oldProjectionFactor;
        private Vector3 oldProjectionCenter;
        private int currentlyHoveredTileId;

        private Edge[] edges;
        private Infrastructure.Infrastructure infrastructure;
        private Fleet.Fleet fleet;

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
            edges = Array.Empty<Edge>();
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

            ProceduralMapGenerator.GenerateMap(this);

            foreach (var chunk in chunks)
            {
                chunk.UpdateMesh();
            }

            Debug.Log($"Generated {tiles.Count} tiles");

            activeTiles = new List<Tile>();
            Shader.SetGlobalFloat(PlanetRadius, radius);

            // Pre-allocate tracking arrays using the total tile capacity count
            Pathfinding.InitBuffers(tiles.Count);
            MovementProfileRegistry.Initialize();
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

                chunk.RenderTrees();
            }

            var edgeMeshData = new List<Edge.EdgeMeshData>();
            foreach(var edge in edges)
            {
                if (edge.MeshChanged) edgeMeshData.Add(edge.RetrieveMeshData());
            }

            UpdateEdgeMesh(edgeMeshData);

            MainCamera.Instance.RequestCurrentlyHoveredTile(OnReadbackComplete);
            MainCamera.Instance.PlanetControllerEnabled = Running;

            // Update the projection
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

        
        public void UpdateEdgeMesh(List<Edge.EdgeMeshData> edgeMeshData)
        {
            // TODO 
        }

        public void ClearHoveredMesh()
        {
            // TODO
        }

        public void SetHoveredMesh(List<Blueprint.HoveredEdge> hoveredEdges)
        {
            // TODO
        }

        public void SetHoveredMesh(Blueprint.HoveredStructure structure)
        {
            // TODO
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


            Infrastructure.SpawnLocal(new Producer.ProducerState
                { Common = { TileId = edges[0].EndTile.Id }, Good = Good.Apple });
        }

        public void Tick()
        {
            if(!Running) return;

            uint tickRate = NetworkManager.Singleton.NetworkTickSystem.TickRate;
            float tickDuration = 1.0f / tickRate;

            Debug.Log("Map Tick");

            foreach (var vehicle in Fleet.Vehicles)
            {
                vehicle.Tick(tickDuration);
            }

            UpdateDirtyObjectsOnClient();

            foreach (var vehicle in Fleet.Vehicles)
            {
                vehicle.ResetProgressDirty();
            }
        }

        private void InitEdges()
        {
            var tempEdges = new List<Edge>();

            foreach (var t in tiles) t.ClearEdges();
            foreach (var t in tiles) t.InitializeEdges(tempEdges);

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

            double time = Time.timeAsDouble;
            // edges
            if (states is Edge.EdgeState[] e) UpdateEdgeStatesClientRpc(timestamp, time, e, rpcParams);

            // structures
            else if (states is Producer.ProducerState[] p) UpdateProducerStatesClientRpc(timestamp, time, p, rpcParams);
            else if (states is Consumer.ConsumerState[] c) UpdateConsumerStatesClientRpc(timestamp, time, c, rpcParams);

            // vehicles
            else if (states is Vehicle.VehicleProgressState[] progress) UpdateVehicleProgressStatesClientRpc(timestamp, time, progress, rpcParams);
            else if (states is Truck.TruckState[] t) UpdateTruckStatesClientRpc(timestamp, time, t, rpcParams);
            else if (states is Freighter.FreighterState[] f) UpdateFreighterStatesClientRpc(timestamp, time, f, rpcParams);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateEdgeStatesClientRpc(Timestamp timestamp, double serverTime, Edge.EdgeState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, serverTime, Edges, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateProducerStatesClientRpc(Timestamp timestamp, double serverTime, Producer.ProducerState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, serverTime, Infrastructure.Producers, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateConsumerStatesClientRpc(Timestamp timestamp, double serverTime, Consumer.ConsumerState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, serverTime, Infrastructure.Consumers, states);

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void UpdateVehicleProgressStatesClientRpc(Timestamp timestamp, double serverTime, Vehicle.VehicleProgressState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, serverTime, Fleet.Vehicles, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateTruckStatesClientRpc(Timestamp timestamp, double serverTime, Truck.TruckState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, serverTime, Fleet.Trucks, states);

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void UpdateFreighterStatesClientRpc(Timestamp timestamp, double serverTime, Freighter.FreighterState[] states, ClientRpcParams rpcParams = default) => UpdateGenericStatesLocal(timestamp, serverTime, Fleet.Freighters, states);



        private void UpdateGenericStatesLocal<T, U>(Timestamp timestamp, double serverTime, IReadOnlyList<T> objects, U[] states) where U : struct, IState where T : Timestamped, ISynchableObject<U>
        {
            if (this.timestamp > timestamp) return;
            this.timestamp = timestamp;
            foreach (var state in states)
            {
                objects[state.ArrayIndex].ApplyServerState(state, serverTime);
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
            UpdateEdgeStatesClientRpc(nextTimestamp, Time.timeAsDouble, edgeStates);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestNewVehicleServerRpc(Vehicle.VehicleType type, TileId parkedTileId, RpcParams rpcParams = default)
        {
            var playerId = PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received new vehicle request from player " + playerId.Value);
            if (playerId == PlayerId.NONE) return;

            int index = fleet.GetFirstEmptyIndex(type, playerId);
            if (index == -1) return;

            if (parkedTileId >= tiles.Count || parkedTileId < 0) return;

            var tile = tiles[parkedTileId];
            if (!tile.CanSpawnVehicle(type)) return;

            Debug.Log("Found free index for vehicle:" + index);

            var commonState = new Vehicle.CommonVehicleState { Index = new((byte)index), Exists = true, ParkedTileId = parkedTileId, RouteIds = null };

            var nextTimestamp = Timestamp.Next();
            if (type == Vehicle.VehicleType.Truck)
                UpdateTruckStatesClientRpc(nextTimestamp, Time.timeAsDouble, new[] { new Truck.TruckState { Common = commonState, Good = Good.None } });
            else
                UpdateFreighterStatesClientRpc(nextTimestamp, Time.timeAsDouble, new[] { new Freighter.FreighterState { Common = commonState } });
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestTruckRouteServerRpc(VehicleIndex index, TileId[] routeIds, RpcParams rpcParams = default)
        {
            var playerId = PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received truck route request from player " + playerId.Value);

            if (playerId == PlayerId.NONE) return;

            var truck = Fleet.Trucks[index];

            if (!truck.IsParked || truck.Owner != playerId) return;
            if (routeIds == null || routeIds.Length < 2) return;

            Tile[] route = new Tile[routeIds.Length];

            for(int i = 0; i < routeIds.Length; i++)
            {
                if (routeIds[i] < 0 || routeIds[i] >= tiles.Count) return;
                route[i] = tiles[routeIds[i]];
            }

            for (int i = 1; i < routeIds.Length; i++)
            {
                if (!Vehicle.CanCross(route[i - 1], route[i], Vehicle.VehicleType.Truck)) return;
            }

            var truckState = truck.State;

            truckState.Common.ParkedTileId = TileId.NONE;
            truckState.Common.RouteIds = routeIds;
            truckState.Common.RouteProgress = 0.0f;

            var nextTimestamp = Timestamp.Next();
            UpdateTruckStatesClientRpc(nextTimestamp, Time.timeAsDouble, new[] { truckState });
        }


        private Vector3 GetProjectedPosition(Vector3 positionOnSphere, float heightOffsetFactor = 1.0f)
        {
            if (oldProjectionCenter == Vector3.zero)
                return positionOnSphere * heightOffsetFactor;

            Vector3 worldPos = positionOnSphere * heightOffsetFactor;
            Vector3 projectionCenter = oldProjectionCenter.normalized;
            float sphereRadius = radius;
            float projectionFactor = oldProjectionFactor;

            Vector3 pNorm = worldPos.normalized;
            float d = Vector3.Dot(projectionCenter, pNorm);
            d = Mathf.Clamp(d, -1.0f, 1.0f);
            float angle = Mathf.Acos(d);

            float arcLength = sphereRadius * angle;

            Vector3 toPoint = pNorm - (projectionCenter * d);
            float lengthToPoint = toPoint.magnitude;

            Vector3 flatPos;
            if (lengthToPoint < 0.0001f)
            {
                flatPos = projectionCenter * sphereRadius;
            }
            else
            {
                Vector3 dirOnPlane = toPoint / lengthToPoint;
                flatPos = (projectionCenter * sphereRadius) + (dirOnPlane * arcLength);
            }

            float elevation = worldPos.magnitude - sphereRadius;
            flatPos += projectionCenter * elevation;

            return Vector3.Lerp(worldPos, flatPos, projectionFactor);
        }

        public void OnDrawGizmos()
        {
            if (edges == null) return;

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

                Vector3 p1 = GetProjectedPosition(edges[i].StartTile.PositionOnSphere, 1.01f);
                Vector3 p2 = GetProjectedPosition(edges[i].EndTile.PositionOnSphere, 1.01f);
                Gizmos.DrawLine(p1, p2);

                if (edges[i].Type != Edge.EdgeType.None)
                {
                    Gizmos.color = Constants.PLAYER_COLORS[edges[i].Owner % Constants.MAX_PLAYER_COUNT];

                    var midPoint = (3 * p1 + p2) / 4.0f;
                    Gizmos.DrawSphere(midPoint, 0.004f);
                    midPoint = (p1 + 3 * p2) / 4.0f;
                    Gizmos.DrawSphere(midPoint, 0.004f);
                }
            }

            var orange = new Color(1.0f, 0.2f, 0.0f);

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
                        Gizmos.DrawSphere(cargoPos, 0.005f);
                    }
                }
            }

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

            foreach (var vehicle in Fleet.Vehicles)
            {
                if (vehicle.PositionOnSphere == null) continue;
                Vector3 basePos = vehicle.PositionOnSphere ?? Vector3.zero;
                Gizmos.color = Constants.PLAYER_COLORS[vehicle.Owner % Constants.MAX_PLAYER_COUNT];
                Gizmos.DrawSphere(GetProjectedPosition(basePos, 1.0f), 0.01f);

                if (vehicle is Truck truck && truck.Good != Good.None)
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

                if (vehicle is Freighter)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(GetProjectedPosition(basePos, 1.014f), 0.005f);
                }
            }
        }
    }
}