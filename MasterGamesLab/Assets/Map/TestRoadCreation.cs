using Map;
using Map.Fleet;
using Map.Infrastructure;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class TestRoadCreation : NetworkBehaviour
{
    ITile startTile = null;

    [SerializeField]
    private Edge.EdgeType type = Edge.EdgeType.Road;

    [SerializeField]
    private Good good = Good.Apple;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var tile = Map.Map.Instance.CurrentlyHovered as Tile;
        if (tile == null) return;

        if (Input.GetKeyDown(KeyCode.P) && IsServer)
        {
            if (tile.CanSpawnStructure(Structure.StructureType.Producer))
            {
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Producer.ProducerState { Common = { TileId = tile.Id }, Good = good });
            }
        }

        if (Input.GetKeyDown(KeyCode.C) && IsServer)
        {
            if (tile.CanSpawnStructure(Structure.StructureType.Consumer))
            {
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Consumer.ConsumerState { Common = { TileId = tile.Id }, RequestedGood = good });
            }
        }

        if (Input.GetKeyDown(KeyCode.I) && IsServer)
        {
            Debug.Log("Spawning port");
            Map.Map.Instance.Infrastructure.SpawnGlobal(new Port.PortState { Common = { TileId = tile.Id } }, new PlayerId(0));
        }

        if (Input.GetKeyDown(KeyCode.O) && IsServer)
        {
            Debug.Log("Spawning garage");
            Map.Map.Instance.Infrastructure.SpawnGlobal(new Garage.GarageState { Common = { TileId = tile.Id } });
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Map.Map.Instance.RequestNewVehicleServerRpc(Map.Fleet.Vehicle.VehicleType.Truck, tile.Id);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Map.Map.Instance.RequestNewVehicleServerRpc(Map.Fleet.Vehicle.VehicleType.Freighter, tile.Id);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            var truck = Map.Map.Instance.Fleet.Trucks.FirstOrDefault(truck => truck.Owner == PlayerManager.Instance.SelfId && truck.IsParked);

            if (truck == null) return;

            TileId[] tileIds = null;

            if (Input.GetKey(KeyCode.LeftShift))
                tileIds = Pathfinding.FindPath(truck.ParkedTile, tile, MovementProfileRegistry.TruckCheapestRoute);
            else
                tileIds = Pathfinding.FindPath(truck.ParkedTile, tile, MovementProfileRegistry.TruckFastestRoute);

            if (tileIds == null) return;

            Map.Map.Instance.RequestVehicleRouteServerRpc(truck.State.ArrayIndex, tileIds);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            var freighter = Map.Map.Instance.Fleet.Freighters.FirstOrDefault(freighter => freighter.Owner == PlayerManager.Instance.SelfId && freighter.IsParked);

            if (freighter == null) return;

            TileId[] tileIds = null;

            if (Input.GetKey(KeyCode.LeftShift))
                tileIds = Pathfinding.FindPath(freighter.ParkedTile, tile, MovementProfileRegistry.FreighterCheapestRoute);
            else
                tileIds = Pathfinding.FindPath(freighter.ParkedTile, tile, MovementProfileRegistry.FreighterFastestRoute);

            if (tileIds == null) return;

            Debug.Log("Freighter Path Length: " + tileIds.Length);

            Map.Map.Instance.RequestVehicleRouteServerRpc(Vehicle.GetOffsetFromType(Vehicle.VehicleType.Freighter) + freighter.Index, tileIds);
        }

        if(Input.GetKeyDown(KeyCode.A)) 
        {
            Debug.Log("Loading truck");
            Map.Map.Instance.LoadFirstTruckOnFreighterServerRpc();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsServer) return;
            Debug.Log("Finishing game");
            Map.Map.Instance.FinishGame();
        }
    }
}
