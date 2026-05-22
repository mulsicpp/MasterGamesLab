using UnityEngine;

public class TestRoadCreation : MonoBehaviour
{
    Map.ITile startTile = null;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            var tile = Map.Map.Instance.GetCurrentlyHoveredTile();
            if (tile == null) return;
            Debug.Log("Clickded on tile with id " + tile.Id.Value);
            if(startTile == null) startTile = tile;
            else
            {
                var endTile = tile;

                Map.Edge edge = null;
                foreach (var e in startTile.Edges)
                {
                    if((e.StartTile == startTile && e.EndTile == endTile) || (e.StartTile == endTile && e.EndTile == startTile))
                    {
                        edge = e;
                        break;
                    }
                }
                if (edge != null && edge.CanBecomeRoad())
                {
                    Debug.Log("Valid edge selected");
                    Map.Map.Instance.RequestNewEdgesServerRpc(Map.Edge.EdgeType.Road, new EdgeId[] { edge.Id });
                }
                startTile = null;
            }
        }
    }
}
