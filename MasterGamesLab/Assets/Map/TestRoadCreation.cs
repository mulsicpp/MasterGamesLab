using Map;
using Map.Infrastructure;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class TestRoadCreation : NetworkBehaviour
{
    Map.ITile startTile = null;

    [SerializeField]
    private Map.Edge.EdgeType type = Map.Edge.EdgeType.Road;

    [SerializeField]
    private Good good = Good.Apple;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;
            Debug.Log("Clickded on tile with id " + tile.Id.Value);
            if (startTile == null) startTile = tile;
            else
            {
                var endTile = tile;

                Map.Edge edge = startTile.FindEdgeTo(endTile);
                if (edge != null && edge.CanBecomeType(type))
                {
                    Debug.Log("Valid edge selected");
                    Map.Map.Instance.RequestNewEdgesServerRpc(type, new EdgeId[] { edge.Id });
                }
                startTile = null;
            }
            if (Map.Map.Instance.testStartTileId == -1)
                Map.Map.Instance.testStartTileId = tile.Id.Value;
            else
                Map.Map.Instance.testTargetTileId = tile.Id.Value;
        }

        if (Input.GetKeyDown(KeyCode.P) && IsServer)
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            if (tile.CanSpawnStructure(Structure.StructureType.Producer))
            {
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Producer.ProducerState { Common = { TileId = tile.Id }, Good = good });
            }
        }

        if (Input.GetKeyDown(KeyCode.C) && IsServer)
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            if (tile.CanSpawnStructure(Structure.StructureType.Consumer))
            {
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Consumer.ConsumerState { Common = { TileId = tile.Id }, RequestedGood = good });
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            Map.Map.Instance.RequestNewVehicleServerRpc(Map.Fleet.Vehicle.VehicleType.Truck, tile.Id);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            Map.Map.Instance.RequestNewVehicleServerRpc(Map.Fleet.Vehicle.VehicleType.Freighter, tile.Id);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            var tile = (Tile)Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            var truck = Map.Map.Instance.Fleet.Trucks.FirstOrDefault(truck => truck.Owner == PlayerManager.Instance.SelfId && truck.IsParked);

            if (truck == null) return;

            TileId[] tileIds = null;

            PlayerId myId = PlayerManager.Instance.SelfId;

            Func<Tile, Tile, long> shortestCost = (Tile t1, Tile t2) =>
            {
                Edge edge = t1.FindEdgeTo(t2);
                if (edge == null || edge.Type != Edge.EdgeType.Road) return -1;

                long primary = (long)Constants.ROAD_MOVEMENT_DISTANCE << 32;
                long secondary = edge.Owner == PlayerId.NONE ? Constants.PUBLIC_ROAD_MOVEMENT_COST :
                                 edge.Owner == myId ? Constants.OWN_ROAD_MOVEMENT_COST :
                                                                 Constants.ENEMY_ROAD_MOVEMENT_COST;

                return primary | (secondary & 0xFFFFFFFFL);
            };

            Func<Tile, Tile, long> cheapestCost = (Tile t1, Tile t2) =>
            {
                Edge edge = t1.FindEdgeTo(t2);
                if (edge == null || edge.Type != Edge.EdgeType.Road) return -1;
                long primary = edge.Owner == PlayerId.NONE ? Constants.PUBLIC_ROAD_MOVEMENT_COST :
                               edge.Owner == myId ? Constants.OWN_ROAD_MOVEMENT_COST :
                                                           Constants.ENEMY_ROAD_MOVEMENT_COST;
                long secondary = (long)Constants.ROAD_MOVEMENT_DISTANCE;

                return (primary << 32) | (secondary & 0xFFFFFFFFL);
            };

            if (Input.GetKey(KeyCode.LeftShift))
                tileIds = Pathfinding.FindPath(truck.ParkedTile, tile, (Tile t1, Tile t2) => { if (t1.FindEdgeTo(t2) != null && t1.FindEdgeTo(t2).Type == Edge.EdgeType.Road) return Constants.ROAD_MOVEMENT_DISTANCE; else return -1; });
            else
                tileIds = Pathfinding.FindPath(truck.ParkedTile, tile, (Tile t1, Tile t2) => { if (t1.FindEdgeTo(t2) != null && t1.FindEdgeTo(t2).Type == Edge.EdgeType.Road) return Constants.ROAD_MOVEMENT_DISTANCE; else return -1; });

            if (tileIds == null) return;

            Map.Map.Instance.RequestTruckRouteServerRpc(truck.Index, tileIds);
        }
    }
}
