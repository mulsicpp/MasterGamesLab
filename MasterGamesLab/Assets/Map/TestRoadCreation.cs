using Map.Infrastructure;
using Unity.Netcode;
using UnityEngine;

public class TestRoadCreation : NetworkBehaviour
{
    Map.ITile startTile = null;

    [SerializeField]
    private Map.Edge.EdgeType type = Map.Edge.EdgeType.Road;

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
                Map.Map.Instance.SpawnStructureGlobal(new Producer.NetData { TileId = tile.Id, Good = Good.Apple });
            }
        }

        if(Input.GetKeyDown(KeyCode.C) && IsServer)
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;

            if(tile.CanSpawnStructure(Structure.StructureType.Consumer))
            {
                Map.Map.Instance.SpawnStructureGlobal(new Consumer.NetData { TileId = tile.Id, RequestedGood = Good.Apple });
            }
        }
    }
}
