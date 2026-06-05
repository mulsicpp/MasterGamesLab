using System;
using Map;
using UnityEngine;
using Map.Infrastructure;
using UnityEngine.InputSystem;
using static Map.Edge;
using Map.Hoverables;

public class ConstructionControls : MonoBehaviour
{
    public enum ConstructionType { None, Hidden, Road, Canal, Garage, Port, Freighter, Truck }

    public event Action<ConstructionType> OnConstructionTypeChanged;

    private InputAction leftClickAction;
    private InputAction cancelAction;
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

            if (type is ConstructionType.None or ConstructionType.Hidden)
                Map.Map.Instance.HoverLayers = HoverablePicker.HoverableLayer.All;
            else
                Map.Map.Instance.HoverLayers = HoverablePicker.HoverableLayer.Tiles;
        }
    }

    public void ToggleHide()
    {
        Type = (Type == ConstructionType.Hidden) ? ConstructionType.None : ConstructionType.Hidden;
    }

    public void OnEnable() 
    { 
        leftClickAction = IngameInputs.leftClickAction;
        cancelAction = IngameInputs.cancelAction;
    }

    public void Update()
    {
        var newTile = Map.Map.Instance.CurrentlyHovered as Tile;

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

        if(Type is ConstructionType.None && Input.GetMouseButtonDown(2))
        {
            switch (Map.Map.Instance.CurrentlyHovered)
            {
                case Tile t:
                    if (t.BlueprintStructure != null)
                        Debug.Log("Structure cost: " + t.BlueprintStructure.BlueprintCost);
                    break;
                case Edge e:
                    if (e.BlueprintType != EdgeType.None)
                        Debug.Log("Edge cost: " + e.BlueprintCost);
                    break;
            }
        }

        if (Type is ConstructionType.None && cancelAction.IsPressed())
        {
            switch(Map.Map.Instance.CurrentlyHovered)
            {
                case Tile t:
                    if(t.BlueprintStructure != null)
                        Map.Map.Instance.Blueprint.RemoveStructure(t.BlueprintStructure);
                    break;
                case Edge e:
                    if(e.BlueprintType != EdgeType.None)
                        Map.Map.Instance.Blueprint.RemoveEdge(e);
                    break;
            }
        }
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