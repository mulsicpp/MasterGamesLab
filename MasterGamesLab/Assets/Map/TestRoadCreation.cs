using Map;
using Map.Fleet;
using Map.Infrastructure;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Player;

public class TestRoadCreation : NetworkBehaviour
{

    [SerializeField]
    private Good good = Good.Apple;

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
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Consumer.ConsumerState { Common = { TileId = tile.Id }, RequestedGood = Good.None });
            }
        }

        if (Input.GetKeyDown(KeyCode.O) && IsServer)
        {
            Map.Map.Instance.Infrastructure.SpawnGlobal(new Garage.GarageState { Common = { TileId = tile.Id } });
        }

        // if (Input.GetKeyDown(KeyCode.L))
        // {
        //     Map.Map.Instance.RequestNewVehicleServerRpc(Map.Fleet.Vehicle.VehicleType.Truck, tile.Id);
        // }
        // 
        // if (Input.GetKeyDown(KeyCode.B))
        // {
        //     Map.Map.Instance.RequestNewVehicleServerRpc(Map.Fleet.Vehicle.VehicleType.Freighter, tile.Id);
        // }

        if (Input.GetKeyDown(KeyCode.D))
        {
            var truck = Map.Map.Instance.Fleet.Trucks.FirstOrDefault(truck => truck.Owner.IsSelf && truck.IsParked);

            if (truck == null) return;

            TileId[] tileIds = null;

            if (Input.GetKey(KeyCode.LeftShift))
                tileIds = Pathfinding.FindPath(truck.ParkedTile, tile, MovementProfileRegistry.TruckCheapestRoute);
            else
                tileIds = Pathfinding.FindPath(truck.ParkedTile, tile, MovementProfileRegistry.TruckFastestRoute);

            if (tileIds == null) return;

            Map.Map.Instance.RequestVehicleRouteServerRpc(truck.IndexInVehicles, tileIds);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            var freighter = Map.Map.Instance.Fleet.Freighters.FirstOrDefault(freighter => freighter.Owner.IsSelf && freighter.IsParked);

            if (freighter == null) return;

            TileId[] tileIds = null;

            if (Input.GetKey(KeyCode.LeftShift))
                tileIds = Pathfinding.FindPath(freighter.ParkedTile, tile, MovementProfileRegistry.FreighterCheapestRoute);
            else
                tileIds = Pathfinding.FindPath(freighter.ParkedTile, tile, MovementProfileRegistry.FreighterFastestRoute);

            if (tileIds == null) return;

            Map.Map.Instance.RequestVehicleRouteServerRpc(freighter.IndexInVehicles, tileIds);
        }
        
        // if(Input.GetKeyDown(KeyCode.A)) 
        // {
        //     Map.Map.Instance.LoadFirstTruckOnFreighterServerRpc();
        // }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsServer) return;
            Map.Map.Instance.FinishGame();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            var details = Map.Map.Instance.Blueprint.GetDetails();
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsServer) {
            foreach (var player in Map.Map.Instance.Players)
            {
                player.Earn(1000);
            }
        }
    }
}
