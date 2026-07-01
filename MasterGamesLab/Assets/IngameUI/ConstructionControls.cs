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
using UnityEngine.Rendering.Universal;
using UI;
using UnityEngine.UIElements;

public class ConstructionControls : MonoBehaviour, IClickEventHandler, IControls
{
    public enum ConstructionType
    {
        None,
        Hidden,
        Road,
        Canal,
        CarPark,
        Port,
        Freighter,
        Truck
    }

    public event Action<ConstructionType> OnConstructionTypeChanged;

    private Tile startTile = null;
    public Tile StartTile
    {
        get { return startTile; }
        set
        {
            if (startTile != null)
                startTile?.ClearOutline();
            startTile = value;
        }
    }

    private bool previewIsValid = false;
    private bool deleting = false;

    [SerializeField] private ConstructionType type = ConstructionType.None;

    public ConstructionType Type
    {
        get => type;
        set
        {
            if (type != ConstructionType.None && type == value)
            {
                Type = ConstructionType.None;
                // OnConstructionTypeChanged?.Invoke(type);
                return;
            }

            type = value;
            StartTile = null;
            deleting = false;
            OnConstructionTypeChanged?.Invoke(type);
            Map.Map.Instance.Blueprint.ClearPreview();


            Map.Map.Instance.Blueprint.ToggleHide(type != ConstructionType.Hidden);

            if (ControlsAreActive)
            {
                IngameUI.Instance.VehicleControls.DisableControls();
            }
            // 
            // if (type is ConstructionType.None or ConstructionType.Hidden)
            //     Map.Map.Instance.HoverLayers = HoverablePicker.HoverableLayer.All;
            // else
            //     Map.Map.Instance.HoverLayers = HoverablePicker.HoverableLayer.Tiles;
        }
    }

    public bool ControlsAreActive => !(Type is ConstructionType.None or ConstructionType.Hidden);

    public void DisableControls()
    {
        if (Type != ConstructionType.Hidden)
            Type = ConstructionType.None;
    }

    public HoverablePicker.HoverableLayer SelectHoverableLayers() => HoverablePicker.HoverableLayer.Tiles;

    public void UpdateControls()
    {
        previewIsValid = false;
        var hoveredTile = Map.Map.Instance.CurrentlyHovered as Tile;

        HoverState hoverState = HoverState.Valid;

        var edgeType = GetEdgeType();
        var vehicleType = GetVehicleType();
        if (edgeType != EdgeType.None)
        {
            previewIsValid = Map.Map.Instance.Blueprint.SetPreviewEdges(StartTile, hoveredTile, edgeType);

            if(StartTile == null && !isValidStartTile(hoveredTile, Type))
                hoverState = HoverState.Invalid;
            else if (StartTile != null && !previewIsValid)
                hoverState = HoverState.Invalid;
        }
        else if (Type is ConstructionType.Port)
        {
            previewIsValid = Map.Map.Instance.Blueprint.SetPreviewStructure(hoveredTile, Structure.StructureType.Port);

            if (!previewIsValid)
            {
                hoverState = HoverState.Invalid;
            }
        }
        else if (vehicleType is Vehicle.VehicleType type)
        {
            previewIsValid = Map.Map.Instance.Blueprint.SetPreviewVehicle(hoveredTile, type);

            if (!previewIsValid)
            {
                hoverState = HoverState.Invalid;
            }
        }
        else
        {
            Map.Map.Instance.Blueprint.ClearPreview();
        }

        if (edgeType == EdgeType.None)
        {
            StartTile = null;
        }

        if (Type is ConstructionType.None && Input.GetMouseButtonDown(2))
        {
            switch (Map.Map.Instance.CurrentlyHovered)
            {
                case Tile t:
                    if (t.BlueprintStructure != null)
                        Debug.Log("Structure cost: " + t.BlueprintStructure.BlueprintCost);
                    if (t.Structure is Consumer consumer)
                        Debug.Log("Consumer { good = " + consumer.Request.Good.ToString() + ", payout = " + consumer.Request.Payout + " }");
                    break;
                case Edge e:
                    if (e.BlueprintType != EdgeType.None)
                        Debug.Log("Edge cost: " + e.BlueprintCost);
                    break;
                case Vehicle v:
                    Debug.Log("Vehicle info: " + v.Type.ToString() + " owned by " + v.Owner.Name + " with index " + v.Index.Value);
                    break;
            }
        }

        if (Type is ConstructionType.None && deleting)
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

        if (ControlsAreActive)
            Map.Map.Instance.HoverOutliner.HoverState = hoverState;
        StartTile?.ShowOutline(Constants.SELECTED_OUTLINE);
    }

    public bool HandleClick(ClickEventType type)
    {
        if (Type == ConstructionType.Hidden) return false;
        switch(type)
        {
            case ClickEventType.Select:
                if(Type == ConstructionType.None) return false;
                var hoveredTile = Map.Map.Instance.CurrentlyHovered as Tile;
                if (GetEdgeType() != EdgeType.None)
                {
                    if(StartTile == null && isValidStartTile(hoveredTile, Type))
                    {
                        StartTile = hoveredTile;
                    }
                    else if (previewIsValid)
                    {
                        Map.Map.Instance.Blueprint.ApplyPreview();
                        StartTile = hoveredTile;
                    }
                }
                else if (previewIsValid)
                {
                    Map.Map.Instance.Blueprint.ApplyPreview();
                }
                return true;
            case ClickEventType.CancelPressed:
                if(Type == ConstructionType.None)
                {
                    deleting = true;
                } else
                {
                    Type = ConstructionType.None;
                }
                return true;
            case ClickEventType.CancelReleased:
                deleting = false;
                return true;
        }
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

    private Vehicle.VehicleType? GetVehicleType()
    {
        return Type switch
        {
            ConstructionType.Truck => Vehicle.VehicleType.Truck,
            ConstructionType.Freighter => Vehicle.VehicleType.Freighter,
            _ => null
        };
    }


    public void ToggleHide()
    {
        IngameUI.Instance.hideButton.style.backgroundImage = new StyleBackground((Type == ConstructionType.Hidden)? IngameUI.Instance.hidden: IngameUI.Instance.hide);
        Type = (Type == ConstructionType.Hidden) ? ConstructionType.None : ConstructionType.Hidden;
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