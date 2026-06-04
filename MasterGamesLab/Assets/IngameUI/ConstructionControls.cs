using System;
using Map;
using UnityEngine;
using Map.Infrastructure;
using UnityEngine.InputSystem;
using static Map.Edge;

public class ConstructionControls : MonoBehaviour
{
    public enum ConstructionType { None, Hidden, Road, Canal, Garage, Port, Freighter, Truck }

    public event Action<ConstructionType> OnConstructionTypeChanged;

    private InputAction leftClickAction;
    private Tile startTile = null;
    private Tile hoveredTile = null;
    private bool previewIsValidOrNonExistent = true;
    [SerializeField] private ConstructionType type = ConstructionType.None;

    public ConstructionType Type
    {
        get => type;
        set
        {
            if (type == value)
            {
                type = ConstructionType.None;
                OnConstructionTypeChanged?.Invoke(type);
                return;
            }

            type = value;
            startTile = null;
            OnConstructionTypeChanged?.Invoke(type);
            Map.Map.Instance.Blueprint.ClearPreview();
        }
    }

    public void ToggleHide()
    {
        Type = (Type == ConstructionType.Hidden) ? ConstructionType.None : ConstructionType.Hidden;
    }

    public void OnEnable() => leftClickAction = IngameInputs.leftClickAction;

    public void Update()
    {
        var newTile = (Tile)Map.Map.Instance.GetCurrentlyHoveredTile();

        var edgeType = GetEdgeType();
        if (edgeType != EdgeType.None)
        {
            if (hoveredTile != newTile)
                previewIsValidOrNonExistent = startTile == null || Map.Map.Instance.Blueprint.SetPreviewEdges(startTile, newTile, edgeType);

            if (previewIsValidOrNonExistent && newTile != null && leftClickAction.WasPerformedThisFrame())
            {
                if (startTile != null)
                {
                    Map.Map.Instance.Blueprint.ApplyPreview();
                    startTile = newTile;
                }
                else if (isValidStartTile(newTile, Type))
                    startTile = newTile;
            }
        }
        else if (Type is ConstructionType.Port)
        {
            if (hoveredTile != newTile)
                previewIsValidOrNonExistent = Map.Map.Instance.Blueprint.SetPreviewStructure(newTile, Structure.StructureType.Port);

            if (previewIsValidOrNonExistent && leftClickAction.WasPerformedThisFrame())
            {
                Map.Map.Instance.Blueprint.ApplyPreview();
            }
        }
        else
        {
            Map.Map.Instance.Blueprint.ClearPreview();
        }
        hoveredTile = newTile;
    }

    // private bool SetPreviewEdges(Tile tile)
    // {
    //     if (startTile != null)
    //     {
    //         if (tile == null)
    //         {
    //             Map.Map.Instance.Blueprint.ClearPreview();
    //             return false;
    //         }
    // 
    //         var (edgeType, path) = Type switch
    //         {
    //             ConstructionType.Road => (Edge.EdgeType.Road, Pathfinding.FindPath(startTile, tile, MovementProfileRegistry.FindRoadBuildPath)),
    //             ConstructionType.Canal => (Edge.EdgeType.Canal, Pathfinding.FindPath(startTile, tile, MovementProfileRegistry.FindCanalBuildPath)),
    //             _ => (Edge.EdgeType.None, null)
    //         };
    // 
    //         Map.Map.Instance.Blueprint.SetPreviewEdges(path, edgeType);
    //         return path?.Length > 1;
    //     }
    // 
    //     Map.Map.Instance.Blueprint.ClearPreview();
    //     return true;
    // }

    private bool SetPreviewStructure(Tile tile)
    {
        // if (tile != null && type == ConstructionType.Port)
        // {
        //     if(tile.CanSpawnStructure(Structure.StructureType.Port)) {
        //         Map.Map.Instance.Blueprint.SetPreviewStructure(tile.Id, Structure.StructureType.Port);
        //         return true;
        //     }
        // }
        Map.Map.Instance.Blueprint.ClearPreview();
        return false;
    }

    private EdgeType GetEdgeType()
    {
        return Type switch
        {
            ConstructionType.Road => EdgeType.Road,
            ConstructionType.Canal => EdgeType.Canal,
            _ => EdgeType.None
        };
    }

    public void ConfirmConstruction()
    {
        Debug.Log("Confirming construction: Applying blueprint changes.");

        Map.Map.Instance.Blueprint.Submit();

        Type = ConstructionType.None;
    }

    public void CancelConstruction()
    {
        Debug.Log("Canceling construction: Reverting preview adjustments.");

        Map.Map.Instance.Blueprint.Clear();
        
        Type = ConstructionType.None;
    }

    private bool isValidStartTile(Tile tile, ConstructionType type)
    {
        if (tile == null) return false;

        switch (type)
        {
            case ConstructionType.Road:
                if (tile.Type == Tile.TileType.Forest || tile.Type == Tile.TileType.Plain)
                    return true;
                break;
            case ConstructionType.Canal:
                if (tile.Type == Tile.TileType.Water || tile.CountEdgesWith(e => e.Type == EdgeType.Canal || e.BlueprintType == EdgeType.Canal) > 0)
                    return true;
                break;
            case ConstructionType.Port:
                break;
        }
        return false;
    }
}