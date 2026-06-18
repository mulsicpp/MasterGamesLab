using InGameCamera;
using Map.Blueprint;
using Map.Fleet;
using Map.GeometryGeneration;
using Map.Hoverables;
using Map.Infrastructure;
using Networking;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Map
{
    public class Map : NetworkBehaviour, IMap
    {
        public const int ID_OFFSET = 1;
        public static int TileLayer { get; private set; }
        public static int EdgeLayer { get; private set; }
        public static int EdgeOutlineLayer { get; private set; }
        public static int EdgeOutlineTransparentLayer { get; private set; }
        public static int VehicleLayer { get; private set; }
        public static int VehicleOutlineLayer { get; private set; }
        public static int VehicleOutlineTransparentLayer { get; private set; }

        private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
        private static readonly int ProjectionFactor = Shader.PropertyToID("_ProjectionFactor");
        private static readonly int ProjectionCenter = Shader.PropertyToID("_ProjectionCenter");

        public static Map Instance { get; private set; } = null!;

        public IReadOnlyList<ITile> Tiles => tiles;
        public IReadOnlyList<ITile> ActiveTiles => activeTiles;
        public float Radius => radius;
        public int Resolution => resolution;

        public float TEST_ROAD_HANDLE_DISTANCE = 0.025f;
        public float TEST_ROAD_HEIGHT = 0.01f;
        public float TEST_CANAL_INSET_LOWER = 0.7f;
        public float TEST_CANAL_INSET_UPPER = 0.6f;
        public float TEST_CANAL_RANDOM_ = 0.00f;

        private float oldTestCanalInsetLower;
        private float oldTestCanalInsetUpper;
        private float oldTestCanalRandom;

        public Timestamp Timestamp = new Timestamp(0);

        public bool SimulationIsRunning => UIManager.Instance?.CurrentMenu == UI.Menu.MenuId.Ingame;

        public IReadOnlyList<Player.Player> Players => players;

        public IReadOnlyList<Edge> Edges => edges;
        public IReadOnlyInfrastructure Infrastructure => infrastructure;
        public IReadOnlyFleet Fleet => fleet;

        public EntityIdManager EntityIdManager { get; private set; }

        public int? GenerationSeed { get; private set; } = null;

        public Blueprint.Blueprint Blueprint;

        private BlueprintPacket[] storedBlueprintPackets;

        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private GameObject edgeGeometryPrefab;

        public GameObject TruckPrefab;
        public GameObject FreighterPrefab;
        
        public GameObject ProducerPrefab;
        public GameObject ConsumerPrefab;
        public GameObject GaragePrefab;
        public GameObject PortPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        public HoverablePicker.HoverableLayer HoverLayers = HoverablePicker.HoverableLayer.All;

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

        public bool isOverUI = false;
        public IHoverable CurrentlyHovered;
        public OutlineCurrentlyHovered HoverOutliner;

        private Player.Player[] players;

        private Edge[] edges;
        private Infrastructure.Infrastructure infrastructure;
        private Fleet.Fleet fleet;

        public ReliableSender ReliableSender;
        public UnreliableSender UnreliableSender;

        private void Awake()
        {
            Instance = this;

            Generate(GenerationSeed ?? 0);
        }

        private void OnEnable()
        {
            TileLayer = LayerMask.NameToLayer("Tiles");
            EdgeLayer = LayerMask.NameToLayer("Edges");
            EdgeOutlineLayer = LayerMask.NameToLayer("Edge Outline");
            EdgeOutlineTransparentLayer = LayerMask.NameToLayer("Edge Outline Transparent");
            VehicleLayer = LayerMask.NameToLayer("Vehicles");
            VehicleOutlineLayer = LayerMask.NameToLayer("Vehicles Outline");
            VehicleOutlineTransparentLayer = LayerMask.NameToLayer("Vehicles Outline Transparent");
        }

        private void UpdateEntireMesh()
        {
            foreach (var chunk in chunks) chunk.UpdateMesh();

            foreach (var tile in tiles) tile.BuildGeometryData();
            foreach (var edge in edges) edge.ChangeVisualState();
            foreach (var structure in infrastructure.Structures) structure.RebuildRenderer();
        }

        private void Update()
        {
            if (TEST_CANAL_INSET_LOWER != oldTestCanalInsetLower || TEST_CANAL_RANDOM_ != oldTestCanalRandom ||
                TEST_CANAL_INSET_UPPER != oldTestCanalInsetUpper)
            {
                oldTestCanalInsetLower = TEST_CANAL_INSET_LOWER;
                oldTestCanalRandom = TEST_CANAL_RANDOM_;
                oldTestCanalInsetUpper = TEST_CANAL_INSET_UPPER;
                TileGeometryFactory.SetCanalInset(TEST_CANAL_INSET_UPPER, TEST_CANAL_INSET_LOWER, TEST_CANAL_RANDOM_);
                foreach (var chunk in chunks)
                {
                    chunk.UpdateMesh();
                }
            }

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

            foreach (var structure in Infrastructure.Structures)
            {
                if (structure.RendererRebuildTriggered)
                {
                    structure.RebuildRenderer();
                }
            }

            UpdateHovered();
            // MainCamera.Instance.PlanetControllerEnabled = Simu;

            // Update the projection
            UpdateProjectionUniforms();

            ClientUpdate();
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

        private void UpdateHovered()
        {
            if (isOverUI)
            {
                return;
            }
            var mousePos = Mouse.current.position.ReadValue();
            var mX = (int)mousePos.x;
            var mY = (int)mousePos.y;

            if (mX < 0 || mX >= Screen.width || mY < 0 || mY >= Screen.height)
            {
                return;
            }
            HoverablePicker.Instance.RequestPick(new Vector2Int(mX, mY), OnReadbackComplete);
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            if (isOverUI)
            {
                return;
            }
            if (request.hasError)
            {
                CurrentlyHovered = null;
                return;
            }

            var colorData = request.GetData<uint>();
            var currentlyHoveredId = (int)colorData[0] - ID_OFFSET;

            if (currentlyHoveredId <= -1)
            {
                CurrentlyHovered = null;
                return;
            }

            CurrentlyHovered = EntityIdManager[new(currentlyHoveredId)] as IHoverable;

            // if (currentlyHoveredId < tiles.Count)
            // {
            //     CurrentlyHovered = tiles[currentlyHoveredId];
            // }
            // else if (currentlyHoveredId < GetTileAndEdgeCount())
            // {
            //     CurrentlyHovered = edges[currentlyHoveredId - tiles.Count];
            // }
            // else if (currentlyHoveredId < GetTileAndEdgeCount() + 100)
            // {
            //     CurrentlyHovered = Fleet.Vehicles[currentlyHoveredId - GetTileAndEdgeCount()];
            // }
        }

        public int GetTileAndEdgeCount() => tiles.Count + edges.Length;

        public Player.PlayerStats[] GetPlayerStats()
        {
            var stats = new Player.PlayerStats[Players.Count];
            for (int i = 0; i < Players.Count; i++)
            {
                stats[i] = new();
                stats[i].Id = Players[i].Id;
                stats[i].Name = Players[i].Name;
                stats[i].Color = Players[i].Color;
                stats[i].Cash = Players[i].Cash;
                stats[i].Revenue = Players[i].Revenue;
            }

            foreach (var edge in Edges)
            {
                if (edge.Owner != null)
                {
                    switch (edge.Type)
                    {
                        case Edge.EdgeType.Road: stats[edge.Owner.Id].RoadCount++; break;
                        case Edge.EdgeType.Canal: stats[edge.Owner.Id].CanalCount++; break;
                    }
                }
            }

            foreach (var port in Infrastructure.Ports)
            {
                if (port.Exists) stats[port.Owner.Id].PortCount++;
            }

            foreach (var truck in Fleet.Trucks)
            {
                if (truck.Exists) stats[truck.Owner.Id].TruckCount++;
            }

            foreach (var freighter in Fleet.Freighters)
            {
                if (freighter.Exists) stats[freighter.Owner.Id].FreighterCount++;
            }

            return stats;
        }

        public void Generate(int seed)
        {
            Debug.Log("Generating world with seed: " + seed);

            GameObject[] children = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i).gameObject;
            }

            transform.DetachChildren();

            foreach (GameObject child in children)
            {
                Destroy(child);
            }

            CurrentlyHovered = null;
            HoverOutliner = GetComponent<OutlineCurrentlyHovered>();

            var (chunksPoints, numPoints) = HexagonalSphere.GenerateIcoSphereChunks(radius, resolution);
            tiles = new List<Tile>(numPoints);
            chunks = new List<MapChunk>(chunksPoints.Count);

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

            // foreach (var chunk in chunks) chunk.UpdateMesh();

            activeTiles = new List<Tile>();
            Shader.SetGlobalFloat(PlanetRadius, radius);

            Timestamp = new Timestamp(0);

            UnityEngine.Random.InitState(seed);
            ProceduralMapGenerator.GenerateMap(this);

            InitEdges();

            players = new Player.Player[0];
            infrastructure = new Infrastructure.Infrastructure(0);
            fleet = new Fleet.Fleet(0);

            EntityIdManager = new();

            Blueprint = new Blueprint.Blueprint();
            storedBlueprintPackets = new BlueprintPacket[0];
            for (int i = 0; i < storedBlueprintPackets.Length; i++)
                storedBlueprintPackets[i] = new BlueprintPacket();

            UpdateEntireMesh();

            Debug.Log($"Generated {tiles.Count} tiles");

            // Pre-allocate tracking arrays using the total tile capacity count
            Pathfinding.InitBuffers(tiles.Count);
            MovementProfileRegistry.Initialize();

            ReliableSender = new ReliableSender(true);
            UnreliableSender = new UnreliableSender();

            GenerationSeed = seed;
        }
        
        public void GeneratePlayersAndStructures(int playerCount)
        {
            players = new Player.Player[playerCount];
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = new Player.Player(new PlayerId((byte)i));
            }

            infrastructure = new Infrastructure.Infrastructure(playerCount);
            fleet = new Fleet.Fleet(playerCount);

            EntityIdManager = new();

            Blueprint = new Blueprint.Blueprint();
            storedBlueprintPackets = new BlueprintPacket[playerCount];
            for (int i = 0; i < storedBlueprintPackets.Length; i++)
                storedBlueprintPackets[i] = new BlueprintPacket();

            var playerSpawnTiles = SpawnPointGenerator.GetFairSpawnPoints(this, players.Length);
        
            for (int i = 0; i < players.Length; i++)
            {
                Infrastructure.SpawnLocal(new Garage.GarageState { Common = { TileId = playerSpawnTiles[i].Id } });
                Fleet.SpawnLocal(
                    new Truck.TruckState
                    {
                        Common = { Exists = true, ParkedTileId = playerSpawnTiles[i].Id },
                        FreighterIndex = VehicleIndex.NONE,
                        Good = Good.None
                    }, players[i]);
            }
        
            spawnPointManager = new ProducerConsumerSpawnPoint(this);

            ////5 producer
            //for (int i = 0; i < 5; i++)
            //{
            //    var prodTile = spawnPointManager.GetSpawnTileProducer();
            //    if (prodTile != null)
            //    {
            //        Infrastructure.SpawnLocal(new Producer.ProducerState
            //        {
            //            Common = { TileId = prodTile.Id },
            //            Good = (Good)UnityEngine.Random.Range((int)Good.Apple, (int)Good.Banana + 1)
            //        });
            //        spawnPointManager.RegisterProducerSpawned(prodTile);
            //    }
            //}

            ////5 consumer (groups)
            //for (int i = 0; i < 5; i++)
            //{
            //    var consTiles = spawnPointManager.GetSpawnTileConsumer();
            //    if (consTiles != null && consTiles.Count > 0)
            //    {
            //        foreach (var consTile in consTiles)
            //        {
            //            Infrastructure.SpawnLocal(new Consumer.ConsumerState { Common = { TileId = consTile.Id } });
            //        }

            //        spawnPointManager.RegisterConsumerSpawned(consTiles);
            //    }
            //}
        }

        private void ClientUpdate()
        {
            if (SimulationIsRunning && IsClient)
            {
                foreach (var vehicle in Fleet.Vehicles)
                {
                    vehicle.EvaluateGameObjectPresence();
                }
            }
        }

        public void FixedUpdate()
        {
            if (!SimulationIsRunning) return;

            if (IsServer)
            {
                foreach (var consumer in Infrastructure.Consumers)
                {
                    consumer.Tick(Time.fixedDeltaTime);
                }

                foreach (var vehicle in Fleet.Vehicles)
                {
                    vehicle.Tick(Time.fixedDeltaTime);
                }

                UpdateDirtyObjectsOnClient();
            }

            if (IsClient)
            {
                foreach (var vehicle in Fleet.Vehicles)
                {
                    vehicle.ClientTick(Time.fixedDeltaTime);
                }
            }
        }

        public void Tick()
        {
            UpdateUnreliableDataOnClient();
        }

        public void FinishGame()
        {
            if (!IsServer) return;
            StartCoroutine(UIManager.Instance.FinishGame());
        }

        private void InitEdges()
        {
            var tempEdges = new List<Edge>();

            foreach (var t in tiles) t.ClearEdges();
            foreach (var t in tiles) t.InitializeEdges(tempEdges);
            foreach (var t in tiles) t.SortEdges();

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

        public void SyncClientMap(Timestamp clientTimestamp, ClientId clientId)
        {
            if (!IsServer) return;

            var sender = new ReliableSender(false, clientId);
            Predicate<Timestamped> condition = obj => obj.ChangedSince(clientTimestamp);

            sender.AddObjects<Player.Player, Player.Player.PlayerState>(players, condition);

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

            Predicate<Timestamped> condition = obj => obj.DirtyCheckAndReset();

            ReliableSender.AddObjects<Player.Player, Player.Player.PlayerState>(players, condition);

            ReliableSender.AddObjects<Edge, Edge.EdgeState>(edges, condition);

            ReliableSender.AddObjects<Producer, Producer.ProducerState>(infrastructure.Producers, condition);
            ReliableSender.AddObjects<Consumer, Consumer.ConsumerState>(infrastructure.Consumers, condition);
            ReliableSender.AddObjects<Garage, Garage.GarageState>(infrastructure.Garages, condition);
            ReliableSender.AddObjects<Port, Port.PortState>(infrastructure.Ports, condition);

            ReliableSender.AddObjects<Truck, Truck.TruckState>(fleet.Trucks, condition);
            ReliableSender.AddObjects<Freighter, Freighter.FreighterState>(fleet.Freighters, condition);

            ReliableSender.Send();
        }

        public void UpdateUnreliableDataOnClient()
        {
            if (!IsServer) return;

            Predicate<Vehicle> routeProgressCondition = v =>
            {
                if (!v.Dirty && v.ProgressDirty)
                {
                    v.ResetProgressDirty();
                    return true;
                }

                return false;
            };

            UnreliableSender.AddObjects<Vehicle, Vehicle.VehicleProgressState>(fleet.Vehicles, routeProgressCondition);
            UnreliableSender.Send();
        }


        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void ApplyReliableStatesClientRpc(
            Timestamp timestamp,
            double serverTime,
            Player.Player.PlayerState[] players,
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


            ApplyStatesLocal(serverTime, this.players, players);

            ApplyStatesLocal(serverTime, this.edges, edges);

            ApplyStatesLocal(serverTime, Infrastructure.Producers, producers);
            ApplyStatesLocal(serverTime, Infrastructure.Consumers, consumers);
            ApplyStatesLocal(serverTime, Infrastructure.Garages, garages);
            ApplyStatesLocal(serverTime, Infrastructure.Ports, ports);

            if (edges.Length + producers.Length + consumers.Length + ports.Length + garages.Length + trucks.Length +
                freighters.Length > 0)
            {
                Blueprint.Validate();
            }

            ApplyStatesLocal(serverTime, Fleet.Trucks, trucks);
            ApplyStatesLocal(serverTime, Fleet.Freighters, freighters);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        public void ApplyUnreliableStatesClientRpc(double serverTime, Vehicle.VehicleProgressState[] vehicleProgresses,
            ClientRpcParams rpcParams = default)
        {
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
        public void SendBlueprintPacketServerRpc(BlueprintPacket.EdgeData[] edges,
            BlueprintPacket.StructureData[] structures,
            BlueprintPacket.VehicleData[] vehicles,
            bool hasNext,
            RpcParams rpcParams = default)
        {
            var player =
                Player.PlayerManager.Instance.GetPlayerFromClientId(new ClientId(rpcParams.Receive.SenderClientId));
            if (player == null) return;

            storedBlueprintPackets[player.Id].Append(new BlueprintPacket(edges, structures, vehicles));

            if (!hasNext)
            {
                var packet = storedBlueprintPackets[player.Id];

                var validatableBlueprint = new ServerValidatableBlueprint(packet);

                validatableBlueprint.Validate();
                int cost = validatableBlueprint.GetDetails().TotalCost;

                if (cost > player.Cash)
                {
                    packet.Clear();
                    return;
                }
                player.Pay(cost);

                // TODO validation
                foreach (var edgeData in packet.Edges)
                {
                    if (edgeData.EdgeId < 0 || edgeData.EdgeId > Edges.Count) continue;
                    var edge = Edges[edgeData.EdgeId];

                    if (edge.Type == Edge.EdgeType.None && validatableBlueprint.IsValid(edge))
                    {
                        edge.Type = edgeData.Type;
                        edge.Owner = player;
                    }
                }

                foreach (var structureData in packet.Structures)
                {
                    if (structureData.TileId < 0 || structureData.TileId > tiles.Count) continue;
                    var tile = Tiles[structureData.TileId] as Tile;

                    var structure = Infrastructure[structureData.StructureId];

                    if (tile.Structure == null && validatableBlueprint.IsValid(structure) &&
                        structure.Owner.Id == player.Id)
                    {
                        structure.Tile = tile;
                    }
                }

                foreach (var vehicleData in packet.Vehicles)
                {
                    if (vehicleData.TileId < 0 || vehicleData.TileId > tiles.Count) continue;
                    var tile = Tiles[vehicleData.TileId] as Tile;

                    var vehicle = Fleet[vehicleData.VehicleId];

                    if (validatableBlueprint.IsValid(vehicle) && vehicle.Owner.Id == player.Id)
                    {
                        vehicle.Exists = true;
                        vehicle.ParkedTile = tile;
                    }
                }


                var responseRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { rpcParams.Receive.SenderClientId },
                    }
                };

                packet.Clear();

                BlueprintAcknowledgementClientRpc(responseRpcParams);

                UpdateDirtyObjectsOnClient();
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestNewVehicleServerRpc(Vehicle.VehicleType type, TileId parkedTileId,
            RpcParams rpcParams = default)
        {
            var player =
                Player.PlayerManager.Instance.GetPlayerFromClientId(new ClientId(rpcParams.Receive.SenderClientId));

            if (player == null) return;

            var vehicle = fleet.GetFirstWith(type, v => !v.Exists && v.Owner.Id == player.Id);

            if (parkedTileId >= tiles.Count || parkedTileId < 0) return;

            var tile = tiles[parkedTileId];
            if (!tile.CanSpawnVehicle(type)) return;

            vehicle.Exists = true;
            vehicle.ParkedTile = tile;

            UpdateDirtyObjectsOnClient();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestVehicleRouteServerRpc(int vehicleIndex, TileId[] routeIds, RpcParams rpcParams = default)
        {
            var player =
                Player.PlayerManager.Instance.GetPlayerFromClientId(new ClientId(rpcParams.Receive.SenderClientId));

            if (player == null) return;
            if (vehicleIndex < 0 || vehicleIndex >= Fleet.Vehicles.Count) return;

            var vehicle = Fleet.Vehicles[vehicleIndex];

            if (routeIds == null || routeIds.Length < 2) return;
            Tile[] route = new Tile[routeIds.Length];

            for (int i = 0; i < routeIds.Length; i++)
            {
                if (routeIds[i] < 0 || routeIds[i] >= tiles.Count) return;
                route[i] = tiles[routeIds[i]];
            }

            if (vehicle.CanDriveRoute(player, route, out var publicCost, out var enemyCosts))
            {
                vehicle.Route = route;
                vehicle.RouteProgress = 0;
                player.Pay(publicCost);
                foreach (var (p, c) in enemyCosts)
                {
                    player.TransferMoneyTo(p, c);
                }
            }


            UpdateDirtyObjectsOnClient();
        }


        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void LoadTruckOnFreighterServerRpc(VehicleIndex truckIndex, VehicleIndex freighterIndex, RpcParams rpcParams = default)
        {
            var player =
                Player.PlayerManager.Instance.GetPlayerFromClientId(new ClientId(rpcParams.Receive.SenderClientId));

            if (player == null) return;

            if (truckIndex < 0 || truckIndex >= Fleet.Trucks.Count) return;
            if (freighterIndex < 0 || freighterIndex >= Fleet.Freighters.Count) return;

            var truck = Fleet.Trucks[truckIndex];
            var freighter = Fleet.Freighters[freighterIndex];

            if (freighter.CanLoadTruck(player, truck, out int cost))
            {
                player.TransferMoneyTo(truck.ParkedTile.Structure.Owner, cost);

                truck.Freighter = freighter;
            }
            UpdateDirtyObjectsOnClient();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        public void UnoadTruckOnPortServerRpc(VehicleIndex freighterIndex, TileId tileId, RpcParams rpcParams = default)
        {
            var player =
                Player.PlayerManager.Instance.GetPlayerFromClientId(new ClientId(rpcParams.Receive.SenderClientId));

            if (player == null) return;

            if (freighterIndex < 0 || freighterIndex >= Fleet.Freighters.Count) return;
            if (tileId < 0 || tileId >= Tiles.Count) return;

            var freighter = Fleet.Freighters[freighterIndex];
            var tile = Tiles[tileId] as Tile;

            if (freighter.CanUnloadTruck(player, tile, out int cost))
            {
                var truck = freighter.Truck;
                truck.Freighter = null;
                truck.ParkedTile = tile;

                player.TransferMoneyTo(tile.Structure.Owner, cost);
            }
            UpdateDirtyObjectsOnClient();
        }


        public Vector3 GetProjectedPosition(Vector3 positionOnSphere, float heightOffsetFactor = 1.0f)
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

        public VehicleTransform GetProjectedVehicleTransform(VehicleTransform transform)
        {
            if (transform == null) return null;
            const float DELTA = 0.001f;

            var projPos = GetProjectedPosition(transform.Position);
            var projPosAbove = GetProjectedPosition(transform.Position + transform.Up * DELTA);
            var projPosInfront = GetProjectedPosition(transform.Position + transform.Forward * DELTA);
            var projPosBehind = GetProjectedPosition(transform.Position - transform.Forward * DELTA);

            return new VehicleTransform
            {
                Position = projPos,
                Up = (projPosAbove - projPos).normalized,
                Forward = (projPosInfront - projPosBehind).normalized,
            };
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
                    default:
                        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.05f);
                        break;
                }

                Vector3 p1 = GetProjectedPosition(edges[i].StartTile.PositionOnSphere, 1.01f);
                Vector3 p2 = GetProjectedPosition(edges[i].EndTile.PositionOnSphere, 1.01f);
                Gizmos.DrawLine(p1, p2);

                if (edges[i].Type != Edge.EdgeType.None)
                {
                    Gizmos.color = edges[i].Owner?.Color ?? Color.white;

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
                    Gizmos.color = port.Owner?.Color ?? Color.darkGray;
                    Gizmos.DrawSphere(basePos, 0.025f);
                }
                else if (port.BlueprintTile != null)
                {
                    Vector3 basePos = GetProjectedPosition(port.BlueprintTile.PositionOnSphere, 1.015f);
                    Gizmos.color = port.BlueprintVisualState switch
                    {
                        VisualState.Preview => Color.purple,
                        VisualState.PreviewOverlapping => Color.blue,
                        VisualState.Valid => Color.cyan,
                        VisualState.Overlapping => Color.green,
                        _ => Color.red,
                    };
                    Gizmos.DrawWireSphere(basePos, 0.025f);
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
                if (vehicle.Transform == null) continue;
                Vector3 basePos = vehicle.Transform.Position;
                if (vehicle.BlueprintTile != null)
                {
                    Gizmos.color = vehicle.BlueprintVisualState switch
                    {
                        VisualState.Preview => Color.purple,
                        VisualState.PreviewOverlapping => Color.blue,
                        VisualState.Valid => Color.cyan,
                        VisualState.Overlapping => Color.green,
                        _ => Color.red,
                    };
                    Gizmos.DrawWireSphere(GetProjectedPosition(basePos, 1.01f), 0.015f);
                }
                else
                {
                    Gizmos.color = vehicle.Owner.Color;
                    Gizmos.DrawSphere(GetProjectedPosition(basePos, 1.01f), 0.015f);
                }

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


            // //debug map
            // if (debugSpawnPoints != null)
            // {
            //     for (int i = 0; i < debugSpawnPoints.Length; i++)
            //     {
            //         var spawnTile = debugSpawnPoints[i];
            //         if (spawnTile != null)
            //         {
            //             Gizmos.color = Color.magenta;
            //             Vector3 debugPos = GetProjectedPosition(spawnTile.PositionOnSphere, 1.05f);
            //             Gizmos.DrawSphere(debugPos, 0.04f);
            // 
            //             Vector3 groundPos = GetProjectedPosition(spawnTile.PositionOnSphere, 1.0f);
            //             Gizmos.DrawLine(groundPos, debugPos);
            //         }
            //     }
            // }
            // 
            // //debug producer/consumer spawn manager
            // if (spawnPointManager != null)
            // {
            //     Gizmos.color = Color.green;
            //     foreach (var tile in spawnPointManager.ValidProducerTiles)
            //     {
            //         Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.03f);
            //         Gizmos.DrawSphere(pos, 0.01f);
            //     }
            // 
            //     foreach (var tile in spawnPointManager.PlacedProducers)
            //     {
            //         Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.06f);
            //         Gizmos.DrawSphere(pos, 0.035f);
            //     }
            // 
            //     Gizmos.color = Color.yellow;
            //     foreach (var tile in spawnPointManager.ValidConsumerTiles)
            //     {
            //         Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.04f);
            //         Gizmos.DrawSphere(pos, 0.008f);
            //     }
            // 
            //     foreach (var tile in spawnPointManager.PlacedConsumers)
            //     {
            //         Vector3 pos = GetProjectedPosition(tile.PositionOnSphere, 1.07f);
            //         Gizmos.DrawSphere(pos, 0.03f);
            //     }
            // }
        }
    }
}