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
    private Good good = Good.Common;

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
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Consumer.ConsumerState { Common = { TileId = tile.Id }, Request = new(Good.None, 0) });
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

        if (Input.GetKeyDown(KeyCode.M) && IsServer) {
            foreach (var player in Map.Map.Instance.Players)
            {
                player.Earn(1000);
            }
        }
    }
}
