using Map;
using UnityEngine;
using Map.Infrastructure;
using UnityEngine.InputSystem;

public class ConstructionControls : MonoBehaviour
{
    public enum ConstructionType
    {
        None,
        Road,
        Canal,
        Garage,
        Port
    }

    [SerializeField] private InputActionAsset inputActions;
    private InputAction leftClickAction;

    private Tile startTile = null;

    [SerializeField] private ConstructionType type;
    public ConstructionType Type
    {
        get { return type; }
        set { type = value; startTile = null; }
    }

    public void OnEnable()
    {
        leftClickAction = inputActions.FindActionMap("Controls").FindAction("LeftClick");
    }

    public void Update()
    {
        var tile = (Tile)Map.Map.Instance.GetCurrentlyHoveredTile();
        if (Type is ConstructionType.Road or ConstructionType.Canal)
        {
            SetPreviewEdges();
            if(tile != null && leftClickAction.WasPerformedThisFrame())
            {
                Debug.Log("Map click");
                Map.Map.Instance.Blueprint.ApplyPreview();
                startTile = tile;
            }
        }
        else if (Type is ConstructionType.Port)
        {
            SetPreviewStructure();
        }
        else
        {
            Map.Map.Instance.Blueprint.ClearPreviewStructure();
            Map.Map.Instance.Blueprint.ClearPreviewEdges();
        }
    }

    private void SetPreviewEdges()
    {
        if (startTile != null)
        {
            var endTile = (Tile)Map.Map.Instance.GetCurrentlyHoveredTile();
            if (endTile == null)
            {
                Map.Map.Instance.Blueprint.ClearPreviewEdges();
                return;
            }

            //var edgeType = Type == ConstructionType.Road ? Edge.EdgeType.Road : Edge.EdgeType.Canal;
            var (edgeType, path) = Type switch
            {
                ConstructionType.Road => (Edge.EdgeType.Road, Pathfinding.FindPath(startTile, endTile, MovementProfileRegistry.FindRoadBuildPath)),
                ConstructionType.Canal => (Edge.EdgeType.Canal, Pathfinding.FindPath(startTile, endTile, MovementProfileRegistry.FindCanalBuildPath)),
                _ => (Edge.EdgeType.None, null)
            };

            Map.Map.Instance.Blueprint.SetPreviewEdges(path, edgeType);
        }
        else
        {
            Map.Map.Instance.Blueprint.ClearPreviewEdges();
            Map.Map.Instance.Blueprint.ClearPreviewStructure();
        }
    }

    private void SetPreviewStructure()
    {
        // var tile = (Tile)Map.Map.Instance.GetCurrentlyHoveredTile();
        // if (tile != null)
        // {
        //     Structure.StructureType? structureType = Type switch
        //     {
        //         ConstructionType.Port => Structure.StructureType.Port,
        //         _ => null
        // 
        //     };
        //     Map.Map.Instance.Blueprint.SetPreviewStructure(structureType != null ? tile.Id : TileId.NONE, structureType ?? Structure.StructureType.Garage);
        // }
        Map.Map.Instance.Blueprint.ClearPreviewEdges();
        Map.Map.Instance.Blueprint.ClearPreviewStructure();
    }


}
