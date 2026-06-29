using Map;
using Map.Fleet;
using Map.GeometryGeneration.Edges;
using Map.Hoverables;
using Map.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using static ConstructionControls;
using static Map.Edge;

namespace UI
{
    public class VehicleControls : MonoBehaviour, IClickEventHandler, IControls
    {
        public abstract class HoveredAction
        {
            public abstract bool IsValid { get; }

            protected VehicleControls controls;

            public abstract bool Commit();

            public HoveredAction(VehicleControls controls)
            {
                this.controls = controls;
            }
        }

        public class HoveredInvalidAction : HoveredAction
        {
            public override bool IsValid => false;

            public HoveredInvalidAction(VehicleControls controls) : base(controls) { }

            public override bool Commit() => false;
        }

        public class HoveredSelectVehicle : HoveredAction
        {
            public override bool IsValid => true;

            private Vehicle vehicle;

            public HoveredSelectVehicle(VehicleControls controls, Vehicle vehicle) : base(controls)
            {
                this.vehicle = vehicle;
            }

            public override bool Commit()
            {
                controls.SelectedVehicle = vehicle;
                return true;
            }
        }

        public class HoveredSelectDestination : HoveredAction
        {
            public override bool IsValid => destination != null;

            private TileId[] fastestRoute = null;
            private TileId[] cheapestRoute = null;
            private Tile destination = null;
            private Tile loadTile = null;

            public HoveredSelectDestination(VehicleControls controls, Tile start, Tile destination) : base(controls)
            {
                var vehicle = controls.SelectedVehicle;


                if (vehicle == null) return;

                var fastestProfile = vehicle.Type == Vehicle.VehicleType.Truck ? MovementProfileRegistry.TruckFastestRoute : MovementProfileRegistry.FreighterFastestRoute;
                var cheapestProfile = vehicle.Type == Vehicle.VehicleType.Truck ? MovementProfileRegistry.TruckCheapestRoute : MovementProfileRegistry.FreighterCheapestRoute;


                if (vehicle is Truck && destination.Type == Tile.TileType.Water)
                {
                    Predicate<Tile> condition = t => t.Structure?.Type == Structure.StructureType.Port && t.Neighbors.Contains(destination);
                    var fastestRoute = Pathfinding.FindPath(start, condition, fastestProfile);
                    var cheapestRoute = Pathfinding.FindPath(start, condition, cheapestProfile);


                    if (fastestRoute != null || cheapestRoute != null)
                    {
                        this.fastestRoute = fastestRoute;
                        this.cheapestRoute = cheapestRoute;
                        this.destination = Map.Map.Instance.Tiles[fastestRoute?[^1] ?? cheapestRoute[^1]] as Tile;
                        this.loadTile = destination;
                    }
                }
                else
                {
                    var fastestRoute = Pathfinding.FindPath(start, destination, fastestProfile);
                    var cheapestRoute = Pathfinding.FindPath(start, destination, cheapestProfile);


                    if (fastestRoute != null || cheapestRoute != null)
                    {
                        this.fastestRoute = fastestRoute;
                        this.cheapestRoute = cheapestRoute;
                        this.destination = destination;
                    }
                }
            }

            public override bool Commit()
            {
                controls.RouteOptions.Set(controls.SelectedVehicle, destination, fastestRoute, cheapestRoute, loadTile);
                return true;
            }
        }

        public class HoveredLoadTruck : HoveredAction
        {
            public override bool IsValid => destination != null;

            private Tile destination = null;

            public HoveredLoadTruck(VehicleControls controls, Tile start, Tile destination) : base(controls)
            {
                if (controls.SelectedVehicle is not Truck) return;

                if (start == null || destination == null) return;

                if (start.Structure == null || start.Structure.Type != Structure.StructureType.Port) return;
                if (destination.Type != Tile.TileType.Water || !destination.Neighbors.Contains(start)) return;

                this.destination = destination;
            }

            public override bool Commit()
            {
                controls.SelectedVehicle.EnqueueAction(new VehicleAction(VehicleAction.ActionType.LoadTruck, destination.Id));
                return true;
            }
        }

        public class HoveredUnloadTruck : HoveredAction
        {
            public override bool IsValid => destination != null;

            private Tile destination = null;

            public HoveredUnloadTruck(VehicleControls controls, Tile start, Tile destination) : base(controls)
            {
                if (controls.SelectedVehicle is not Truck) return;

                if (destination?.Structure == null || destination.Structure.Type != Structure.StructureType.Port) return;

                var path = Pathfinding.FindPath(start, t => t.Neighbors.Contains(destination), MovementProfileRegistry.FreighterFastestRoute);

                if (path != null)
                {
                    this.destination = destination;
                }
            }

            public override bool Commit()
            {
                controls.SelectedVehicle.EnqueueAction(new VehicleAction(VehicleAction.ActionType.UnloadTruck, destination.Id));
                return true;
            }
        }

        public class HoveredWaitForTruck : HoveredAction
        {
            public override bool IsValid => destination != null;

            private Tile destination = null;

            public HoveredWaitForTruck(VehicleControls controls, Tile start, Tile destination) : base(controls)
            {
                if (controls.SelectedVehicle is not Freighter) return;

                if (start == null || destination == null) return;

                if (destination.Structure == null || destination.Structure.Type != Structure.StructureType.Port) return;
                if (start.Type != Tile.TileType.Water || !start.Neighbors.Contains(destination)) return;

                this.destination = destination;
            }

            public override bool Commit()
            {
                controls.SelectedVehicle.EnqueueAction(new VehicleAction(VehicleAction.ActionType.WaitForTruck, destination.Id));
                return true;
            }
        }

        public class HoveredSelectRoute : HoveredAction
        {
            public override bool IsValid => true;

            private Route.RouteType Type;

            public HoveredSelectRoute(VehicleControls controls, Route.RouteType type) : base(controls)
            {
                Type = type;
            }

            public override bool Commit()
            {
                controls.ChooseRoute(Type);
                return true;
            }
        }

        public bool ControlsAreActive => SelectedVehicle != null;

        private HoveredAction hoveredAction = null;

        private Vehicle selectedVehicle = null;
        public Vehicle SelectedVehicle
        {
            get { return selectedVehicle; }
            set
            {
                if (selectedVehicle == value) return;
                if (selectedVehicle != null)
                {
                    selectedVehicle.ClearOutline();
                    selectedVehicle.OnActionQueueChanged -= ActionQueueChanged;
                }
                selectedVehicle = value;
                RouteOptions.Clear();

                if (selectedVehicle != null)
                {
                    selectedVehicle.OnActionQueueChanged += ActionQueueChanged;
                }
                BuildActionQueueGameObjects();

                if (ControlsAreActive)
                {
                    IngameUI.Instance.ConstructionControls.DisableControls();
                }
            }
        }

        public RouteOptions RouteOptions { get; private set; }

        private List<GameObject> actionQueueGameObjects;
        private Route currentRoute;

        public void Start()
        {
            RouteOptions = new();
            actionQueueGameObjects = new();
            currentRoute = new(Route.RouteType.Current);
            currentRoute.Renderer.PinVisible = false;
        }

        public void DisableControls()
        {
            hoveredAction = null;
            SelectedVehicle = null;
            RouteOptions.Clear();
        }

        public HoverablePicker.HoverableLayer SelectHoverableLayers() => HoverablePicker.HoverableLayer.All;

        public void UpdateControls()
        {
            hoveredAction = null;
            if (SelectedVehicle == null)
            {
                switch (Map.Map.Instance.CurrentlyHovered)
                {
                    case Vehicle v:
                        hoveredAction = new HoveredSelectVehicle(this, v); break;

                }
            }
            else
            {
                SelectedVehicle.ShowOutline(Constants.SELECTED_OUTLINE);
                var start = SelectedVehicle.GetTileLocationAfterAllActions(out bool loaded);
                RouteOptions.VisualDestination?.ShowOutline(Constants.SELECTED_OUTLINE);

                switch (Map.Map.Instance.CurrentlyHovered)
                {
                    case Tile t:
                        if (!SelectedVehicle.Owner.IsSelf) break;

                        if (loaded)
                        {
                            hoveredAction = new HoveredUnloadTruck(this, start, t);
                            break;
                        }
                        hoveredAction = new HoveredLoadTruck(this, start, t);
                        if (hoveredAction.IsValid) break;
                        hoveredAction = new HoveredWaitForTruck(this, start, t);
                        if (hoveredAction.IsValid) break;
                        hoveredAction = new HoveredSelectDestination(this, start, t);
                        break;
                    case Vehicle v:
                        if (SelectedVehicle != v)
                        {
                            hoveredAction = new HoveredSelectVehicle(this, v);
                        }
                        break;
                    case RouteGeometry r:
                        if (SelectedVehicle.Owner.IsSelf)
                            hoveredAction = new HoveredSelectRoute(this, r.Type);
                        break;
                }
            }

            if (ControlsAreActive)
                Map.Map.Instance.HoverOutliner.HoverState = hoveredAction?.IsValid ?? true ? HoverState.Valid : HoverState.Invalid;

            RouteOptions.UpdateFacingDirections();
            if (SelectedVehicle?.Route != null)
            {
                currentRoute.SetRoute(SelectedVehicle, SelectedVehicle.Route.Select(t => t.Id).ToArray());
            }
            else
            {
                currentRoute.SetRoute(null, null);
            }
        }


        public bool HandleClick(ClickEventType type)
        {
            switch (type)
            {
                case ClickEventType.Select:
                    if (hoveredAction?.IsValid ?? false)
                        return hoveredAction.Commit();
                    return false;
                case ClickEventType.CancelPressed:
                    if (!ControlsAreActive) return false;
                    DisableControls();
                    return true;
            }
            return false;
        }

        public void ChooseRoute(Route.RouteType type)
        {
            if (SelectedVehicle != null && RouteOptions.Destination != null)
            {
                TileId[] routeIds = type switch
                {
                    Route.RouteType.Cheapest => RouteOptions.CheapestRoute.TileIds ?? RouteOptions.FastestRoute.TileIds,
                    _ => RouteOptions.FastestRoute.TileIds ?? RouteOptions.CheapestRoute.TileIds,
                };

                if (routeIds != null)
                {
                    SelectedVehicle.EnqueueAction(new VehicleAction(VehicleAction.ActionType.DriveRoute, RouteOptions.Destination.Id, routeIds));
                    if (RouteOptions.LoadTile != null)
                    {
                        SelectedVehicle.EnqueueAction(new VehicleAction(VehicleAction.ActionType.LoadTruck, RouteOptions.LoadTile.Id));
                    }
                    RouteOptions.Clear();
                }
            }
        }

        private void ActionQueueChanged()
        {
            BuildActionQueueGameObjects();
        }

        public void BuildActionQueueGameObjects()
        {
            Debug.Log("Building action queue game objects");
            IngameUI.Instance.setActionQueueVisible(true);

            foreach (var go in actionQueueGameObjects)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            actionQueueGameObjects.Clear();
            IngameUI.Instance.ClearActionQueue();

            if (SelectedVehicle == null || SelectedVehicle.ActionQueue.Count == 0)
            {
                IngameUI.Instance.setActionQueueVisible(false);
                return;
            }

            foreach (var action in SelectedVehicle.ActionQueue)
            {
                if (action.Type == VehicleAction.ActionType.DriveRoute)
                {
                    Route r = new(Route.RouteType.Queued);
                    r.SetRoute(SelectedVehicle, action.RouteIds);
                    r.Renderer.PinVisible = false;
                    actionQueueGameObjects.Add(r.Renderer.gameObject);
                }
                else
                {
                    actionQueueGameObjects.Add(null);
                }

                IngameUI.VehicleAction uiAction = action.Type switch
                {
                    VehicleAction.ActionType.LoadTruck => IngameUI.VehicleAction.LoadTruck,
                    VehicleAction.ActionType.UnloadTruck => IngameUI.VehicleAction.UnloadTruck,
                    VehicleAction.ActionType.WaitForTruck => IngameUI.VehicleAction.WaitFreighter,
                    _ => SelectedVehicle.Type == Vehicle.VehicleType.Truck ?
                        IngameUI.VehicleAction.DriveTruck : IngameUI.VehicleAction.DriveFreighter
                };
                IngameUI.Instance.AddItemToQueue(uiAction);
            }
        }

        public void OnDrawGizmos()
        {
            var map = Map.Map.Instance;
            if (SelectedVehicle != null)
            {
                Gizmos.color = Color.orange.linear;

                Gizmos.DrawSphere(map.GetProjectedPosition(SelectedVehicle.GetTileLocationAfterAllActions(out _)?.PositionOnSphere ?? Vector3.zero, 1.01f), 0.015f);

                if (SelectedVehicle.IsDriving)
                {
                    for (int i = 1; i < SelectedVehicle.Route.Length; i++)
                    {
                        var p1 = map.GetProjectedPosition(SelectedVehicle.Route[i - 1].PositionOnSphere, 1.012f);
                        var p2 = map.GetProjectedPosition(SelectedVehicle.Route[i].PositionOnSphere, 1.012f);
                        Gizmos.DrawLine(p1, p2);
                    }
                }

                var currentNode = SelectedVehicle.ActionQueue.First;

                while (currentNode != null)
                {
                    var action = currentNode.Value;
                    Vector3 basePos;
                    Vector3 prevPos;
                    switch (action.Type)
                    {
                        case VehicleAction.ActionType.DriveRoute:
                            for (int i = 1; i < action.RouteIds.Length; i++)
                            {
                                var p1 = map.GetProjectedPosition(map.Tiles[action.RouteIds[i - 1]].PositionOnSphere, 1.012f);
                                var p2 = map.GetProjectedPosition(map.Tiles[action.RouteIds[i]].PositionOnSphere, 1.012f);
                                Gizmos.DrawLine(p1, p2);
                            }
                            break;
                        case VehicleAction.ActionType.UnloadTruck:
                            basePos = map.Tiles[action.TargetTileId].PositionOnSphere;

                            Gizmos.DrawSphere(map.GetProjectedPosition(basePos, 1.015f), 0.015f);
                            Gizmos.DrawSphere(map.GetProjectedPosition(basePos, 1.035f), 0.01f);
                            Gizmos.DrawSphere(map.GetProjectedPosition(basePos, 1.055f), 0.005f);
                            break;
                        case VehicleAction.ActionType.LoadTruck:
                            var prevTile = SelectedVehicle.GetTileLocationAfterAction(currentNode.Previous, out _);
                            prevPos = prevTile?.PositionOnSphere ?? Vector3.zero;
                            basePos = map.Tiles[action.TargetTileId].PositionOnSphere;

                            Gizmos.DrawSphere(map.GetProjectedPosition(basePos, 1.015f), 0.015f);
                            Gizmos.DrawSphere(map.GetProjectedPosition(0.5f * (basePos + prevPos), 1.025f), 0.01f);
                            Gizmos.DrawSphere(map.GetProjectedPosition(prevPos, 1.015f), 0.005f);
                            break;

                        case VehicleAction.ActionType.WaitForTruck:
                            var tile = SelectedVehicle.GetTileLocationAfterAction(currentNode, out _);
                            basePos = tile?.PositionOnSphere ?? Vector3.zero;
                            prevPos = map.Tiles[action.TargetTileId].PositionOnSphere;

                            Gizmos.DrawSphere(map.GetProjectedPosition(basePos, 1.015f), 0.015f);
                            Gizmos.DrawSphere(map.GetProjectedPosition(0.5f * (basePos + prevPos), 1.025f), 0.01f);
                            Gizmos.DrawSphere(map.GetProjectedPosition(prevPos, 1.015f), 0.005f);
                            break;
                    }
                    currentNode = currentNode.Next;
                }
            }
        }
    }
}