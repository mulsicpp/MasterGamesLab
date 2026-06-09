using System;
using Map;
using UnityEngine;
using Map.Infrastructure;
using UnityEngine.InputSystem;
using static Map.Edge;
using Map.Hoverables;
using Map.OutlineEffect;
using Map.Fleet;
using System.Collections.Generic;
using System.Linq;

public class ConstructionControls : MonoBehaviour
{
    public enum ConstructionType
    {
        None,
        Hidden,
        Road,
        Canal,
        Garage,
        Port,
        Freighter,
        Truck
    }

    public event Action<ConstructionType> OnConstructionTypeChanged;

    private InputAction leftClickAction;
    private InputAction cancelAction;
    private Tile startTile = null;
    // private Tile hoveredTile = null;
    // private bool previewIsValidOrNonExistent = true;
    [SerializeField] private ConstructionType type = ConstructionType.None;
    [SerializeField] private GameObject tileOutlinerPrefab;

    private TileOutliner tileOutliner;
    private TileOutliner startTileOutliner;
    private Edge previouslyHoveredEdge;

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

        tileOutliner = Instantiate(tileOutlinerPrefab).GetComponent<TileOutliner>();
        tileOutliner.SetOutlineParameters(Constants.HOVER_OUTLINE);

        startTileOutliner = Instantiate(tileOutlinerPrefab).GetComponent<TileOutliner>();
        startTileOutliner.SetOutlineParameters(Constants.HOVER_OUTLINE);
    }

    public void Update()
    {
        var hoveredTile = Map.Map.Instance.CurrentlyHovered as Tile;

        var outlineData = Constants.HOVER_OUTLINE;

        var edgeType = GetEdgeType();
        var vehicleType = GetVehicleType();
        if (edgeType != EdgeType.None)
        {
            bool previewIsValidOrNonExistent = startTile == null ||
                                              Map.Map.Instance.Blueprint.SetPreviewEdges(startTile, hoveredTile, edgeType);

            if(startTile == null && !isValidStartTile(hoveredTile, Type))
                outlineData = Constants.ROAD_BLUEPRINT_INVALID_OUTLINE;
            else if (!previewIsValidOrNonExistent)
                outlineData = Constants.ROAD_BLUEPRINT_INVALID_OUTLINE;

            if (previewIsValidOrNonExistent && hoveredTile != null && leftClickAction.WasPerformedThisFrame())
            {
                if (startTile != null)
                {
                    Map.Map.Instance.Blueprint.ApplyPreview();
                    startTile = hoveredTile;
                }
                else if (isValidStartTile(hoveredTile, Type))
                    startTile = hoveredTile;
                else
                    outlineData = Constants.ROAD_BLUEPRINT_INVALID_OUTLINE;
            }
        }
        else if (Type is ConstructionType.Port)
        {
            bool previewIsValid = Map.Map.Instance.Blueprint.SetPreviewStructure(hoveredTile, Structure.StructureType.Port);

            if (!previewIsValid)
            {
                outlineData = Constants.ROAD_BLUEPRINT_INVALID_OUTLINE;
            }

            if (previewIsValid && leftClickAction.WasPerformedThisFrame())
            {
                Map.Map.Instance.Blueprint.ApplyPreview();
            }
        }
        else if (vehicleType is Vehicle.VehicleType type)
        {
            bool previewIsValid = Map.Map.Instance.Blueprint.SetPreviewVehicle(hoveredTile, type);

            if (!previewIsValid)
            {
                outlineData = Constants.ROAD_BLUEPRINT_INVALID_OUTLINE;
            }

            if (previewIsValid && leftClickAction.WasPerformedThisFrame())
            {
                Map.Map.Instance.Blueprint.ApplyPreview();
            }
        }
        else
        {
            Map.Map.Instance.Blueprint.ClearPreview();
        }

        if(edgeType == EdgeType.None)
            startTile = null;

        if (Type is ConstructionType.None && Input.GetMouseButtonDown(2))
        {
            switch (Map.Map.Instance.CurrentlyHovered)
            {
                case Tile t:
                    if (t.BlueprintStructure != null)
                        Debug.Log("Structure cost: " + t.BlueprintStructure.BlueprintCost);
                    if (t.Structure is Consumer consumer)
                        Debug.Log("Consumer { good = " + consumer.RequestedGood.ToString() + ", payout = " + consumer.CurrentPayout + " }");
                    break;
                case Edge e:
                    if (e.BlueprintType != EdgeType.None)
                        Debug.Log("Edge cost: " + e.BlueprintCost);
                    break;
            }
        }

        if (Type is ConstructionType.None && cancelAction.IsPressed())
        {
            switch (Map.Map.Instance.CurrentlyHovered)
            {
                case Tile t:
                    if (t.BlueprintStructure != null)
                        Map.Map.Instance.Blueprint.RemoveStructure(t.BlueprintStructure);
                    var removeList = new List<Vehicle>();
                    foreach (var vehicle in Map.Map.Instance.Blueprint.Vehicles)
                    {
                        if (vehicle.BlueprintTile == t)
                            removeList.Add(vehicle);
                    }
                    foreach (var vehicle in removeList)
                        Map.Map.Instance.Blueprint.RemoveVehicle(vehicle);
                    break;
                case Edge e:
                    if (e.BlueprintType != EdgeType.None)
                        Map.Map.Instance.Blueprint.RemoveEdge(e);
                    break;
            }
        }

        if (startTile != null)
        {
            startTileOutliner.SetOutlineParameters(outlineData);
            startTileOutliner.OutlineTile(startTile);
        }
        else
        {
            startTileOutliner.ClearOutline();
        }


        switch (Map.Map.Instance.CurrentlyHovered)
        {
            case Tile t:
                if (previouslyHoveredEdge != null)
                {
                    previouslyHoveredEdge.EdgeDirty = true;
                    previouslyHoveredEdge = null;
                }

                tileOutliner.SetOutlineParameters(outlineData);
                tileOutliner.OutlineTile(t);
                break;
            case Edge e:
                if (previouslyHoveredEdge != null && previouslyHoveredEdge != e)
                {
                    previouslyHoveredEdge.TriggerDirty();
                }

                tileOutliner.ClearOutline();
                e.SetOutlineParameters(Constants.HOVER_OUTLINE_FILLED_IN, e.Type == EdgeType.Canal);
                previouslyHoveredEdge = e;
                break;
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

    private Vehicle.VehicleType? GetVehicleType()
    {
        return Type switch
        {
            ConstructionType.Truck => Vehicle.VehicleType.Truck,
            ConstructionType.Freighter => Vehicle.VehicleType.Freighter,
            _ => null
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
                if ((tile.Type == Tile.TileType.Water && tile.Neighbors.FirstOrDefault(n => (n as Tile)?.CanBuild(out _) ?? false) != null) ||
                    tile.CountEdgesWith(e => e.Type == EdgeType.Canal || e.BlueprintType == EdgeType.Canal) > 0)
                    return true;
                break;
            case ConstructionType.Port:
                break;
        }

        return false;
    }
}