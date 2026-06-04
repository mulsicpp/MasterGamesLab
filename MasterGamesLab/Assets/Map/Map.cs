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
using Map.Blueprint;
using static UnityEditor.VersionControl.Asset;
using Networking;
using Blueprint;
using UnityEditor.PackageManager;
using Unity.AppUI.Redux;

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

        public Timestamp Timestamp = new Timestamp(0);

        [SerializeField] public bool Running = true;

        public IReadOnlyList<Edge> Edges => edges;
        public IReadOnlyInfrastructure Infrastructure => infrastructure;
        public IReadOnlyFleet Fleet => fleet;

        public int? GenerationSeed { get; private set; } = null;

        public Blueprint.Blueprint Blueprint;

        private BlueprintPacket[] storedBlueprintPackets;

        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private GameObject edgeGeometryPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        //debug
        private ITile[] debugSpawnPoints;
        [SerializeField] private bool renderTrees = true;
        private ProducerConsumerSpawnPoint spawnPointManager;

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

        public ReliableSender ReliableSender;
        public UnreliableSender UnreliableSender;

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
            ReliableSender = new Networking.ReliableSender(true);
            UnreliableSender = new Networking.UnreliableSender();

            Blueprint = new Blueprint.Blueprint();
            storedBlueprintPackets = new BlueprintPacket[4];
            for (int i = 0; i < storedBlueprintPackets.Length; i++)
                storedBlueprintPackets[i] = new BlueprintPacket();

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

            //ProceduralMapGenerator.GenerateMap(this);

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

            //debug
            if (UIManager.Instance == null)
            {
                Generate(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            }

        }

        private void UpdateEntireMesh()
        {
            foreach (var chunk in chunks)
            {
                chunk.UpdateMesh();
                chunk.UpdateTileData();
            }

            foreach (var tile in tiles) tile.BuildGeometryData();
            foreach (var edge in edges) edge.ChangeVisualState();
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

                if (renderTrees)
                {
                    chunk.RenderTrees();
                }
            }

            foreach (var tile in tiles)
            {
                if (tile.GeometryChanged)
                {
                    tile.BuildGeometryData();
                }
            }

            foreach (var edge in edges)
            {
                if (edge.EdgeDirty)
                {
                    edge.ChangeVisualState();
                }
            }

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

        public GameObject GetEdgeGeometryPrefab() => edgeGeometryPrefab;

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

        public void GenerateEmpty()
        {
            foreach (var tile in tiles)
            {
                tile.Type = Tile.TileType.Water;
            }

            InitEdges();

            infrastructure = new Infrastructure.Infrastructure();
            fleet = new Fleet.Fleet();

            UpdateEntireMesh();

            GenerationSeed = null;
        }

        public void Generate(int seed)
        {
            Debug.Log("Generating world with seed " + seed + " ...");

            //foreach (var tile in tiles)
            //{
            //    if (tile.PositionOnSphere.z < -0.97f) tile.Type = Tile.TileType.Mountain;
            //    else if (tile.PositionOnSphere.z < -0.9f) tile.Type = Tile.TileType.Forest;
            //    else if (tile.PositionOnSphere.z < -0.7f) tile.Type = Tile.TileType.Plain;
            //    else tile.Type = Tile.TileType.Water;
            //}

            UnityEngine.Random.InitState(seed);
            ProceduralMapGenerator.GenerateMap(this);

            InitEdges();

            infrastructure = new Infrastructure.Infrastructure();
            fleet = new Fleet.Fleet();

            UpdateEntireMesh();

            ITile[] playerSpawns = SpawnPointGenerator.GetFairSpawnPoints(this, 4);

            for (int i = 0; i < playerSpawns.Length; i++)
            {
                Debug.Log($"Player {i + 1} Spawnpoint: ID {playerSpawns[i].Id} on Continent {playerSpawns[i].ContinentId}");

                // Infrastructure.SpawnLocal(new Producer.ProducerState
                // { Common = { TileId = edges[0].EndTile.Id }, Good = Good.Apple });
            }

            debugSpawnPoints = SpawnPointGenerator.GetFairSpawnPoints(this, 4);

            for (int i = 0; i < debugSpawnPoints.Length; i++)
            {
                Debug.Log($"Player  {i + 1} Spawnpoint: ID {debugSpawnPoints[i].Id} on Continent {debugSpawnPoints[i].ContinentId}");
            }

            //Infrastructure.SpawnLocal(new Producer.ProducerState
            //    { Common = { TileId = edges[0].EndTile.Id }, Good = Good.Apple });

            //debug: Spawn Manager Beispiel
            //var spawnManager = new ProducerConsumerSpawnPoint(this);
            spawnPointManager = new ProducerConsumerSpawnPoint(this);


            //5 producer
            for (int i = 0; i < 5; i++)
            {
                var prodTile = spawnPointManager.GetSpawnTileProducer();
                if (prodTile != null)
                {
                    spawnPointManager.RegisterProducerSpawned(prodTile);
                }
            }

            //5 consumer (groups)
            for (int i = 0; i < 5; i++)
            {
                var consTiles = spawnPointManager.GetSpawnTileConsumer();
                if (consTiles != null && consTiles.Count > 0)
                {
                    spawnPointManager.RegisterConsumerSpawned(consTiles);
                }
            }

            GenerationSeed = seed;
        }

        public void Tick()
        {
            if (!Running) return;

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

        public void FinishGame()
        {
            if(!IsServer) return;
            StartCoroutine(UIManager.Instance.FinishGame());
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

            var sender = new Networking.ReliableSender(false, clientId);
            Predicate<Timestamped> condition = obj => obj.Timestamp > clientTimestamp;

            sender.AddObjects<Edge, Edge.EdgeState>(edges, condition);

            sender.AddObjects<Producer, Producer.ProducerState>(infrastructure.Producers, condition);
            sender.AddObjects<Consumer, Consumer.ConsumerState>(infrastructure.Consumers, condition);
            sender.AddObjects<Garage, Garage.GarageState>(infrastructure.Garages, condition);
            sender.AddObjects<Port, Port.PortState>(infrastructure.Ports, condition);

            sender.AddObjects<Truck, Truck.TruckState>(fleet.Trucks, condition);
            sender.AddObjects<Freighter, Freighter.FreighterState>(fleet.Freighters, condition);

            sender.Send();
        }

        public void UpdateDirtyObjectsOnClient()
        {
            if (!IsServer) return;

            Predicate<Timestamped> condition = obj => obj.Dirty;

            ReliableSender.AddObjects<Edge, Edge.EdgeState>(edges, condition);

            ReliableSender.AddObjects<Producer, Producer.ProducerState>(infrastructure.Producers, condition);
            ReliableSender.AddObjects<Consumer, Consumer.ConsumerState>(infrastructure.Consumers, condition);
            ReliableSender.AddObjects<Garage, Garage.GarageState>(infrastructure.Garages, condition);
            ReliableSender.AddObjects<Port, Port.PortState>(infrastructure.Ports, condition);

            ReliableSender.AddObjects<Truck, Truck.TruckState>(fleet.Trucks, condition);
            ReliableSender.AddObjects<Freighter, Freighter.FreighterState>(fleet.Freighters, condition);

            ReliableSender.Send();


            UnreliableSender.AddObjects<Vehicle, Vehicle.VehicleProgressState>(fleet.Vehicles,
                obj => !obj.Dirty && obj.ProgressDirty);

            UnreliableSender.Send();
        }


        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void ApplyReliableStatesClientRpc(
            Timestamp timestamp,
            double serverTime,
            Edge.EdgeState[] edges,
            Producer.ProducerState[] producers,
            Consumer.ConsumerState[] consumers,
            Garage.GarageState[] garages,
            Port.PortState[] ports,
            Truck.TruckState[] trucks,
            Freighter.FreighterState[] freighters,
            ClientRpcParams rpcParams = default
        )
        {
            Timestamp = timestamp;
            ApplyStatesLocal(serverTime, this.edges, edges);

            ApplyStatesLocal(serverTime, Infrastructure.Producers, producers);
            ApplyStatesLocal(serverTime, Infrastructure.Consumers, consumers);
            ApplyStatesLocal(serverTime, Infrastructure.Garages, garages);
            ApplyStatesLocal(serverTime, Infrastructure.Ports, ports);

            ApplyStatesLocal(serverTime, Fleet.Trucks, trucks);
            ApplyStatesLocal(serverTime, Fleet.Freighters, freighters);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        public void ApplyUnreliableStatesClientRpc(double serverTime, Vehicle.VehicleProgressState[] vehicleProgresses,
            ClientRpcParams rpcParams = default)
        {
            Debug.Log("received unreliable update! state count: " + vehicleProgresses.Length);
            ApplyStatesLocal(serverTime, Fleet.Vehicles, vehicleProgresses);
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void BlueprintAcknowledgementClientRpc(ClientRpcParams rpcParams = default)
        {
            Blueprint.Clear();
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void GameFinishedClientRpc(ClientRpcParams rpcParams = default)
        {
            NetworkManager.Shutdown(false);
            UIManager.Instance.CurrentMenu = UI.Menu.MenuId.GameFinished;
        }

        private void ApplyStatesLocal<T, U>(double serverTime, IReadOnlyList<T> objects,
            U[] states) where U : struct, IState where T : Timestamped, ISynchableObject<U>
        {
            foreach (var state in states)
            {
                objects[state.ArrayIndex].ApplyServerState(state, serverTime);
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void SendBlueprintPacketServerRpc(EdgeId[] roads, EdgeId[] canals, TileId[] ports, bool hasNext,
            RpcParams rpcParams = default)
        {
            var playerId =
                PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received new blueprint packet from player " + playerId.Value);
            if (playerId == PlayerId.NONE) return;

            storedBlueprintPackets[playerId].Append(new BlueprintPacket(roads, canals, ports));

            if(!hasNext)
            {
                var packet = storedBlueprintPackets[playerId];
                Debug.Log("Applying blueprint from player " + playerId.Value);

                // TODO validation
                foreach(var edgeId in packet.RoadEdgeIds)
                {
                    if(edgeId < 0 || edgeId > edges.Length) continue;
                    var edge = edges[edgeId];

                    if(edge.Type == Edge.EdgeType.None)
                    {
                        ReliableSender.Add(new Edge.EdgeState { Id = edgeId, Type = Edge.EdgeType.Road, Owner = playerId });
                    }
                }

                foreach (var edgeId in packet.CanalEdgeIds)
                {
                    if (edgeId < 0 || edgeId > edges.Length) continue;
                    var edge = edges[edgeId];

                    if (edge.Type == Edge.EdgeType.None)
                    {
                        ReliableSender.Add(new Edge.EdgeState { Id = edgeId, Type = Edge.EdgeType.Canal, Owner = playerId });
                    }
                }

                // foreach (var tileId in packet.PortTileIds)
                // {
                //     if (tileId < 0 || tileId > tiles.Count) continue;
                //     var tile = tiles[tileId];
                // 
                //     if (tile.Structure == null)
                //     {
                //         var index = infrastructure.GetFirstEmptyIndex(Structure.StructureType.Port, playerId);
                //         if (index == -1) continue;
                // 
                //         ReliableSender.Add(new Port.PortState { Common = { Index = new StructureIndex((byte)index), TileId = tile.Id } });
                //     }
                // }


                var responseRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { rpcParams.Receive.SenderClientId },
                    }
                };

                BlueprintAcknowledgementClientRpc(responseRpcParams);

                ReliableSender.Send();
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestNewEdgesServerRpc(Edge.EdgeType edgeType, EdgeId[] edgeIds, RpcParams rpcParams = default)
        {
            var playerId =
                PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
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

            ReliableSender.AddStates(edgeStates);
            ReliableSender.Send();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestNewVehicleServerRpc(Vehicle.VehicleType type, TileId parkedTileId,
            RpcParams rpcParams = default)
        {
            var playerId =
                PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received new vehicle request from player " + playerId.Value);
            if (playerId == PlayerId.NONE) return;

            int index = fleet.GetFirstEmptyIndex(type, playerId);
            if (index == -1) return;

            if (parkedTileId >= tiles.Count || parkedTileId < 0) return;

            var tile = tiles[parkedTileId];
            if (!tile.CanSpawnVehicle(type)) return;

            Debug.Log("Found free index for vehicle:" + index);

            var commonState = new Vehicle.CommonVehicleState
                { Index = new((byte)index), Exists = true, ParkedTileId = parkedTileId, RouteIds = null };


            if (type == Vehicle.VehicleType.Truck)
                ReliableSender.Add(new Truck.TruckState { Common = commonState, Good = Good.None, FreighterIndex = VehicleIndex.NONE });
            else
                ReliableSender.Add(new Freighter.FreighterState { Common = commonState });
            ReliableSender.Send();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestVehicleRouteServerRpc(int vehicleIndex, TileId[] routeIds, RpcParams rpcParams = default)
        {
            var playerId =
                PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received vehicle route request from player " + playerId.Value + " for vehicle " + vehicleIndex);

            if (playerId == PlayerId.NONE) return;
            if(vehicleIndex < 0 || vehicleIndex >= Fleet.Vehicles.Count) return;

            var vehicle = Fleet.Vehicles[vehicleIndex];

            if (!vehicle.Exists || !vehicle.IsParked || vehicle.Owner != playerId) return;
            if (routeIds == null || routeIds.Length < 2) return;

            Tile[] route = new Tile[routeIds.Length];

            for (int i = 0; i < routeIds.Length; i++)
            {
                if (routeIds[i] < 0 || routeIds[i] >= tiles.Count) return;
                route[i] = tiles[routeIds[i]];
            }

            Debug.Log("Checking path");
            for (int i = 1; i < routeIds.Length; i++)
            {
                if (!Vehicle.CanCross(route[i - 1], route[i], vehicle.Type)) return;
            }
            Debug.Log("Path OK");

            if (vehicle.Type == Vehicle.VehicleType.Truck)
            {
                var truckState = (vehicle as Truck).State;

                truckState.Common.ParkedTileId = TileId.NONE;
                truckState.Common.RouteIds = routeIds;
                truckState.Common.RouteProgress = 0.0f;

                ReliableSender.Add(truckState);
            }
            else
            {
                var freighterState = (vehicle as Freighter).State;

                freighterState.Common.ParkedTileId = TileId.NONE;
                freighterState.Common.RouteIds = routeIds;
                freighterState.Common.RouteProgress = 0.0f;

                ReliableSender.Add(freighterState);
            }
            ReliableSender.Send();
        }


        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void LoadFirstTruckOnFreighterServerRpc(RpcParams rpcParams = default)
        {
            var playerId =
                PlayerManager.Instance.GetPlayerIdFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            Debug.Log("Received load request from player " + playerId.Value);

            if (playerId == PlayerId.NONE) return;

            var truck = Fleet.Trucks.FirstOrDefault(truck => truck.Owner == playerId);
            var freighter = Fleet.Freighters.FirstOrDefault(freighter => freighter.Owner == playerId);
            Debug.Log("Loading truck" + truck.Index.Value + "onto freighter " + freighter.Index.Value);

            var truckState = truck.State;

            truckState.FreighterIndex = freighter.Index;

            ReliableSender.Add(truckState);
            ReliableSender.Send();
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
            
                if (edges[i].BlueprintType != Edge.EdgeType.None)
                {
                    Gizmos.color = edges[i].BlueprintVisualState switch
                    {
                        VisualState.Preview => Color.purple,
                        VisualState.PreviewOverlapping => Color.blue,
                        VisualState.Valid => Color.cyan,
                        VisualState.Overlapping => Color.green,
                        _ => Color.red,
                    };
            
                    p1 = GetProjectedPosition(edges[i].StartTile.PositionOnSphere, 1.012f);
                    p2 = GetProjectedPosition(edges[i].EndTile.PositionOnSphere, 1.012f);
                    Gizmos.DrawLine(p1, p2);
                }
            }

            var orange = new Color(1.0f, 0.15f, 0.0f);

            foreach (var producer in infrastructure.Producers)
            {
                if (producer.Tile != null)
                {
                    Vector3 basePos = GetProjectedPosition(producer.Tile.PositionOnSphere, 1.015f);
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(basePos, 0.025f);

                    if (producer.Good != Good.None)
                    {
                        switch (producer.Good)
                        {
                            case Good.Apple: Gizmos.color = Color.red; break;
                            case Good.Orange: Gizmos.color = orange; break;
                            case Good.Banana: Gizmos.color = Color.yellow; break;
                        }

                        Vector3 cargoPos = GetProjectedPosition(producer.Tile.PositionOnSphere, 1.03f);
                        Gizmos.DrawSphere(cargoPos, 0.007f);
                    }
                }
            }

            foreach (var consumer in infrastructure.Consumers)
            {
                if (consumer.Tile != null)
                {
                    Vector3 basePos = GetProjectedPosition(consumer.Tile.PositionOnSphere, 1.015f);
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(basePos, 0.025f);

                    if (consumer.RequestedGood != Good.None)
                    {
                        switch (consumer.RequestedGood)
                        {
                            case Good.Apple: Gizmos.color = Color.red; break;
                            case Good.Orange: Gizmos.color = orange; break;
                            case Good.Banana: Gizmos.color = Color.yellow; break;
                        }

                        Vector3 cargoPos = GetProjectedPosition(consumer.Tile.PositionOnSphere, 1.03f);
                        Gizmos.DrawSphere(cargoPos, 0.007f);
                    }
                }
            }

            foreach (var port in infrastructure.Ports)
            {
                if (port.Tile != null)
                {
                    Vector3 basePos = GetProjectedPosition(port.Tile.PositionOnSphere, 1.015f);
                    Gizmos.color = PlayerManager.Instance.GetPlayerColor(port.Owner);
                    Gizmos.DrawSphere(basePos, 0.025f);
                }
            }

            foreach (var garage in infrastructure.Garages)
            {
                if (garage.Tile != null)
                {
                    Vector3 basePos = GetProjectedPosition(garage.Tile.PositionOnSphere, 1.015f);
                    Gizmos.color = Color.brown;
                    Gizmos.DrawSphere(basePos, 0.025f);
                }
            }

            foreach (var vehicle in Fleet.Vehicles)
            {
                if (vehicle.PositionOnSphere == null) continue;
                Vector3 basePos = vehicle.PositionOnSphere ?? Vector3.zero;
                Gizmos.color = Constants.PLAYER_COLORS[vehicle.Owner % Constants.MAX_PLAYER_COUNT];
                Gizmos.DrawSphere(GetProjectedPosition(basePos, 1.01f), 0.015f);

                if (vehicle is Truck truck && truck.Good != Good.None)
                {
                    switch (truck.Good)
                    {
                        case Good.Apple: Gizmos.color = Color.red; break;
                        case Good.Orange: Gizmos.color = orange; break;
                        case Good.Banana: Gizmos.color = Color.yellow; break;
                    }

                    Vector3 cargoPos = GetProjectedPosition(basePos, 1.025f);
                    Gizmos.DrawSphere(cargoPos, 0.007f);
                }

                if (vehicle is Freighter)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(GetProjectedPosition(basePos, 1.014f), 0.005f);
                }
            }

            //debug map
            if (debugSpawnPoints != null)
            {
                for (int i = 0; i < debugSpawnPoints.Length; i++)
                {
                    var spawnTile = debugSpawnPoints[i];
                    if (spawnTile != null)
                    {
                        Gizmos.color = Color.magenta;
                        Vector3 debugPos = GetProjectedPosition(spawnTile.PositionOnSphere, 1.05f);
                        Gizmos.DrawSphere(debugPos, 0.04f);

                        Vector3 groundPos = GetProjectedPosition(spawnTile.PositionOnSphere, 1.0f);
                        Gizmos.DrawLine(groundPos, debugPos);
                    }
                }
            }

            //debug producer/consumer spawn manager
            if (spawnPointManager != null)
            {
                Gizmos.color = Color.green;
                foreach (var tile in spawnPointManager.ValidProducerTiles)
                {
                    Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.03f);
                    Gizmos.DrawSphere(pos, 0.01f);
                }
                foreach (var tile in spawnPointManager.PlacedProducers)
                {
                    Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.06f);
                    Gizmos.DrawSphere(pos, 0.035f);
                }

                Gizmos.color = Color.yellow;
                foreach (var tile in spawnPointManager.ValidConsumerTiles)
                {
                    Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.04f);
                    Gizmos.DrawSphere(pos, 0.008f);
                }
                foreach (var tile in spawnPointManager.PlacedConsumers)
                {
                    Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.07f);
                    Gizmos.DrawSphere(pos, 0.03f);
                }
            }
        }
    }
}