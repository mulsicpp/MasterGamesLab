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
            public override bool IsValid => route != null;

            private TileId[] route;

            public HoveredSelectRoute(VehicleControls controls, Tile destination) : base(controls)
            {
                route = null;
                var vehicle = controls.SelectedVehicle;
                if (vehicle == null || !vehicle.IsParked) return;
                var movementProfile = vehicle.Type == Vehicle.VehicleType.Truck ? MovementProfileRegistry.TruckFastestRoute : MovementProfileRegistry.FreighterFastestRoute;
                route = Pathfinding.FindPath(vehicle.ParkedTile, destination, movementProfile);
            }

            public override bool Commit()
            {
                Map.Map.Instance.RequestVehicleRouteServerRpc(controls.SelectedVehicle.IndexInVehicles, route);
                controls.SelectedVehicle = null;
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
                if (freighter?.CanLoadTruck(truck) ?? false)
                {
                    this.truck = truck;
                    this.freighter = freighter;
                }
            }

            public override bool Commit()
            {
                Map.Map.Instance.LoadTruckOnFreighterServerRpc(truck.Index, freighter.Index);
                controls.SelectedVehicle = null;
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

                if (freighter?.CanUnloadTruck(destination) ?? false)
                {
                    this.freighter = freighter;
                    this.portTile = destination;
                }
            }

            public override bool Commit()
            {
                Map.Map.Instance.UnoadTruckOnPortServerRpc(freighter.Index, portTile.Id);
                controls.SelectedVehicle = null;
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
                if (selectedVehicle != null)
                    selectedVehicle?.ClearOutline();
                selectedVehicle = value;

                if (ControlsAreActive)
                {
                    IngameUI.Instance.ConstructionControls.DisableControls();
                }
            }
        }

        public void DisableControls()
        {
            hoveredAction = null;
            SelectedVehicle = null;
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
            else if(SelectedVehicle != null)
            {
                SelectedVehicle.ShowOutline(Constants.SELECTED_OUTLINE);
                switch (Map.Map.Instance.CurrentlyHovered)
                {
                    case Tile t:
                        hoveredAction = new HoveredSelectRoute(this, t);
                        if (hoveredAction.IsValid) break;
                        hoveredAction = new HoveredUnloadTruck(this, t);
                        break;
                    case Vehicle v:
                        if (SelectedVehicle != v)
                            hoveredAction = new HoveredLoadTruck(this, v);
                        break;
                }
            }

            if(ControlsAreActive)
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
    }
}