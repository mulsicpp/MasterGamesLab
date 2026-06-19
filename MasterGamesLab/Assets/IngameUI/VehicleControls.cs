using Map;
using Map.Fleet;
using Map.Hoverables;
using Map.Infrastructure;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public class HoveredSelectRoute : HoveredAction
        {
            public override bool IsValid => destination != null;

            private TileId[] fastestRoute = null;
            private TileId[] cheapestRoute = null;
            private Tile destination = null;

            public HoveredSelectRoute(VehicleControls controls, Tile destination) : base(controls)
            {
                var vehicle = controls.SelectedVehicle;
                if (vehicle == null) return;
                var fastestProfile = vehicle.Type == Vehicle.VehicleType.Truck ? MovementProfileRegistry.TruckFastestRoute : MovementProfileRegistry.FreighterFastestRoute;
                var cheapestProfile = vehicle.Type == Vehicle.VehicleType.Truck ? MovementProfileRegistry.TruckCheapestRoute : MovementProfileRegistry.FreighterCheapestRoute;
                
                var fastestRoute = Pathfinding.FindPath(vehicle.ParkedTile, destination, fastestProfile);
                var cheapestRoute = Pathfinding.FindPath(vehicle.ParkedTile, destination, cheapestProfile);


                if (vehicle.CanDriveRoute(Player.Player.Self, fastestRoute, out _, out _))
                {
                    this.fastestRoute = fastestRoute;
                    this.destination = destination;
                }

                if (!Route.AreSame(this.fastestRoute, cheapestRoute) && vehicle.CanDriveRoute(Player.Player.Self, cheapestRoute, out _, out _))
                {
                    this.cheapestRoute = cheapestRoute;
                    this.destination = destination;
                }
            }

            public override bool Commit()
            {
                // Map.Map.Instance.RequestVehicleRouteServerRpc(controls.SelectedVehicle.IndexInVehicles, route);
                controls.SelectedDestination = destination;
                controls.routes[0].SetRoute(fastestRoute);
                controls.routes[1].SetRoute(cheapestRoute);
                return true;
            }
        }

        public class HoveredLoadTruck : HoveredAction
        {
            public override bool IsValid => freighter != null && truck != null;

            private Truck truck = null;
            private Freighter freighter = null;

            public HoveredLoadTruck(VehicleControls controls, Vehicle target) : base(controls)
            {
                var truck = controls.SelectedVehicle as Truck;
                var freighter = target as Freighter;
                if (freighter?.CanLoadTruck(Player.Player.Self, truck, out _) ?? false)
                {
                    this.truck = truck;
                    this.freighter = freighter;
                }
            }

            public override bool Commit()
            {
                Map.Map.Instance.LoadTruckOnFreighterServerRpc(truck.Index, freighter.Index);
                controls.SelectedVehicle = freighter;
                return true;
            }
        }

        public class HoveredUnloadTruck : HoveredAction
        {
            public override bool IsValid => portTile != null;

            private Freighter freighter = null;
            private Tile portTile = null;

            public HoveredUnloadTruck(VehicleControls controls, Tile destination) : base(controls)
            {
                var freighter = controls.SelectedVehicle as Freighter;

                if (freighter?.CanUnloadTruck(Player.Player.Self, destination, out _) ?? false)
                {
                    this.freighter = freighter;
                    portTile = destination;
                }
            }

            public override bool Commit()
            {
                var truck = freighter.Truck;
                Map.Map.Instance.UnoadTruckOnPortServerRpc(freighter.Index, portTile.Id);
                controls.SelectedVehicle = truck;
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
                    selectedVehicle?.ClearOutline();
                selectedVehicle = value;
                ClearDestination();



                if (ControlsAreActive)
                {
                    IngameUI.Instance.ConstructionControls.DisableControls();
                }
            }
        }

        private Tile selectedDestination = null;
        public Tile SelectedDestination
        {
            get { return selectedDestination; }
            set
            {
                if (selectedDestination != null)
                {
                    selectedDestination?.ClearOutline();
                }
                selectedDestination = value;


                if (ControlsAreActive)
                {
                    IngameUI.Instance.ConstructionControls.DisableControls();
                }
            }
        }

        private Route[] routes;

        public void Awake()
        {
             routes = new Route[] { new Route(null, Color.red), new Route(null, Color.green) };
        }

        public void DisableControls()
        {
            hoveredAction = null;
            SelectedVehicle = null;
            ClearDestination();
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

                if (SelectedDestination == null)
                {

                    switch (Map.Map.Instance.CurrentlyHovered)
                    {
                        case Tile t:
                            hoveredAction = new HoveredSelectRoute(this, t);
                            if (hoveredAction.IsValid) break;
                            hoveredAction = new HoveredUnloadTruck(this, t);
                            break;
                        case Vehicle v:
                            if (SelectedVehicle != v)
                            {
                                hoveredAction = new HoveredLoadTruck(this, v);
                                if (hoveredAction.IsValid) break;
                                hoveredAction = new HoveredSelectVehicle(this, v);
                            }
                            break;
                    }
                } else
                {
                    SelectedDestination.ShowOutline(Constants.SELECTED_OUTLINE);

                    switch (Map.Map.Instance.CurrentlyHovered)
                    {
                        case Tile t:
                            hoveredAction = new HoveredSelectRoute(this, t);
                            if (hoveredAction.IsValid) break;
                            hoveredAction = new HoveredUnloadTruck(this, t);
                            break;
                        case Vehicle v:
                            if (SelectedVehicle != v)
                            {
                                hoveredAction = new HoveredLoadTruck(this, v);
                                if (hoveredAction.IsValid) break;
                                hoveredAction = new HoveredSelectVehicle(this, v);
                            }
                            break;
                    }
                }
            }

            if (ControlsAreActive)
                Map.Map.Instance.HoverOutliner.HoverState = hoveredAction?.IsValid ?? true ? HoverState.Valid : HoverState.Invalid;
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

        public void ClearDestination()
        {
            SelectedDestination = null;
            routes[0].SetRoute(null);
            routes[1].SetRoute(null);
        }

        public void ChooseFastestRoute()
        {
            if(SelectedVehicle != null && SelectedDestination != null)
            {
                TileId[] routeIds = routes[0].Tiles ?? routes[1].Tiles;
                if (routeIds != null)
                {
                    Map.Map.Instance.RequestVehicleRouteServerRpc(SelectedVehicle.IndexInVehicles, routeIds);
                    ClearDestination();
                }
            }
        }

        public void ChooseCheapestRoute()
        {
            if (SelectedVehicle != null && SelectedDestination != null)
            {
                TileId[] routeIds = routes[1].Tiles ?? routes[0].Tiles;
                if (routeIds != null)
                {
                    Map.Map.Instance.RequestVehicleRouteServerRpc(SelectedVehicle.IndexInVehicles, routeIds);
                    ClearDestination();
                }
            }
        }
    }
}