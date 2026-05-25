using Map;
using Map.Infrastructure;
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

                Map.Edge edge = null;
                foreach (var e in startTile.Edges)
                {
                    if ((e.StartTile == startTile && e.EndTile == endTile) || (e.StartTile == endTile && e.EndTile == startTile))
                    {
                        edge = e;
                        break;
                    }
                }
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

        if(Input.GetKeyDown(KeyCode.P) && IsServer)
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            if(tile.CanSpawnStructure(Structure.StructureType.Producer))
            {
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Producer.ProducerState { Common = { TileId = tile.Id }, Good = good });
            }
        }

        if(Input.GetKeyDown(KeyCode.C) && IsServer)
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            if(tile.CanSpawnStructure(Structure.StructureType.Consumer))
            {
                Map.Map.Instance.Infrastructure.SpawnGlobal(new Consumer.ConsumerState { Common = { TileId = tile.Id }, RequestedGood = good });
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            Map.Map.Instance.RequestNewTruckServerRpc(tile.Id);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            var tile = (Tile)Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            var truck = Map.Map.Instance.Fleet.Trucks.First(truck => truck.Owner == PlayerManager.Instance.SelfId && truck.IsParked);

            TileId[] tileIds;

            Map.Map.Instance.FindShortestPath(truck.ParkedTile, tile, out tileIds);

            if (tileIds == null) return;

            Map.Map.Instance.RequestTruckRouteServerRpc(truck.Index, tileIds);
        }
    }
}
